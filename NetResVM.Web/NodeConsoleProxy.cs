using System.Net.WebSockets;
using System.Text;
using BusinessLayer.DTOs;
using BusinessLayer.Interface;
using SimpleLogger;

namespace NetResVM.Web
{
    /// <summary>
    /// Poskytuje obousměrný komunikační most (WebSocket Reverse Proxy / Protocol Bridge) mezi webovým terminálem
    /// v prohlížeči uživatele (<c>xterm.js</c>) a sériovou konzolí (CLI) konkrétního síťového prvku
    /// běžícího na virtualizační platformě Cisco Modeling Labs (CML) nebo EVE-NG.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Architektonický kontext:</strong><br/>
    /// Webový prohlížeč klienta se z bezpečnostních a síťových důvodů nemůže připojovat přímo k virtualizačnímu
    /// serveru (CML / EVE-NG):
    /// <list type="number">
    ///   <item>
    ///     <description><strong>Izolace přihlašovacích údajů:</strong> Přístupové údaje (administrátorské jméno, heslo a JWT token)
    ///     k virtualizačnímu serveru jsou uloženy výhradně v databázi rezervačního systému a nesmí být nikdy odeslány
    ///     do prohlížeče koncového uživatele (studenta).</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>Překlad protokolů (Protocol Translation):</strong> Webové prohlížeče nativně nepodporují surová
    ///     TCP spojení (Telnet) ani protokol SSH. Komunikují přes protokol WebSocket (RFC 6455). Na straně ASP.NET Core backendu
    ///     proto přijímáme WebSocket rámce z knihovny <c>xterm.js</c> a delegujeme je přes rozhraní <see cref="IVirtualizationAdapter"/>
    ///     do příslušného adaptéru platformy (<c>ApiCisco</c> / <c>ApiEVE</c>), který je překládá do SSH PTY streamu
    ///     (u Cisco CML) nebo surového TCP/Telnet socketu (u EVE-NG).</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>Eliminace problémů s CORS a self-signed TLS certifikáty:</strong> Školní virtualizační servery
    ///     typicky využívají certifikáty podepsané vlastní autoritou (self-signed), které by prohlížeč při přímém
    ///     WebSocket spojení zablokoval. Backendové spojení tento problém řeší na úrovni serveru.</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>Dodržení vícevrstvé architektury (3-Tier + Adapter Pattern):</strong> Prezentační vrstva
    ///     (<c>NetResVM.Web</c>) pouze spravuje životní cyklus HTTP/WebSocket spojení s prohlížečem a samotnou komunikaci
    ///     s virtualizačním serverem deleguje přes <see cref="IVirtualizationAdapter.ConnectNodeConsoleAsync"/>.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Podporované komunikační režimy (implementované v adaptérech <c>ApiCisco</c> a <c>ApiEVE</c>):</strong>
    /// <list type="bullet">
    ///   <item>
    ///     <description><strong>Cisco CML – Primární režim (SSH Terminal Server, port 22):</strong> Využívá vestavěný konzolový
    ///     multiplexer CML (<c>virl2-terminal-server</c>), kde po navázání SSH relace a otevření pseudo-terminálu (PTY)
    ///     automaticky odešle příkaz <c>open /{labId}/{nodeId}/{line}</c> pro přepnutí do sériové linky daného routeru/switche.</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>Cisco CML – Záložní režim (HTTPS/WSS Dispatcher, port 443):</strong> Pokud je port 22 blokován
    ///     firewallem, adaptér automaticky přejde na REST API (<c>/api/v0/labs/{labId}/nodes/{nodeId}/keys/console</c> nebo
    ///     <c>/api/v0/keys/console</c>) a naváže interní CML WebSocket spojení na
    ///     <c>wss://&lt;host&gt;/ws/dispatch/frontend/console?uuid=&lt;uuid&gt;</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>EVE-NG – Telnet Proxy režim:</strong> Z metadat uzlu (<c>UrlConnect</c>) zjistí dynamicky
    ///     přidělený TCP port běžícího QEMU/IOL/Dynamips kontejneru a propojí jej s WebSocketem přes <c>TcpClient</c>
    ///     včetně filtrace řídicích sekvencí Telnet IAC (<c>0xFF</c>).</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class NodeConsoleProxy
    {
        /// <summary>
        /// Instance loggeru pro zaznamenávání chyb a varování během životního cyklu konzolové relace.
        /// </summary>
        private static readonly SimpleLogger.ILogger Logger = FileLogger.Instance;

        /// <summary>
        /// Hlavní vstupní bod pro obsluhu interaktivní konzolové relace jednoho síťového prvku.
        /// Metoda řídí životní cyklus WebSocket spojení, deleguje navázání relace na příslušný adaptér
        /// virtualizační platformy (<see cref="IVirtualizationAdapter"/>) a zajišťuje korektní
        /// uzavření WebSocketu při odpojení uživatele.
        /// </summary>
        /// <param name="browserWs">Aktivní WebSocket spojení mezi prohlížečem uživatele (<c>xterm.js</c>) a naším ASP.NET Core serverem.</param>
        /// <param name="server">DTO objekt reprezentující cílový virtualizační server (obsahuje ID a typ platformy CML/EVE).</param>
        /// <param name="labId">Unikátní identifikátor laboratoře na virtualizačním serveru (např. UUID v CML nebo cesta k <c>.unl</c> v EVE-NG).</param>
        /// <param name="nodeId">Identifikátor konkrétního síťového prvku uvnitř laboratoře (např. UUID/<c>n0</c> v CML nebo číselné ID v EVE-NG).</param>
        /// <param name="line">Číslo sériové konzolové linky prvku (výchozí je <c>0</c> pro primární konzolový port Console 0).</param>
        /// <param name="cols">Počet sloupců terminálu (šířka ve znacích) vypočtený na frontendu doplňkem <c>xterm-addon-fit</c>.</param>
        /// <param name="rows">Počet řádků terminálu (výška ve znacích) vypočtený na frontendu doplňkem <c>xterm-addon-fit</c>.</param>
        /// <param name="adapter">Adaptér virtualizační platformy (<see cref="IVirtualizationAdapter"/>) zajišťující komunikaci s CML/EVE-NG.</param>
        /// <param name="cancellationToken">Token signalizující přerušení HTTP/WebSocket požadavku ze strany klienta nebo serveru.</param>
        /// <returns>Asynchronní úloha (<see cref="Task"/>), která trvá po celou dobu otevřené relace terminálu.</returns>
        public static async Task HandleConsoleSessionAsync(
            WebSocket browserWs,
            ServerDTO server,
            string labId,
            string nodeId,
            int line,
            uint cols,
            uint rows,
            IVirtualizationAdapter adapter,
            CancellationToken cancellationToken)
        {
            try
            {
                // Delegování navázání konzolové relace do adaptéru příslušné virtualizační platformy (CiscoCmlAdapter / EveNGAdapter)
                await adapter.ConnectNodeConsoleAsync(
                    server.Id,
                    labId,
                    nodeId,
                    browserWs,
                    line,
                    cols,
                    rows,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Očekávané ukončení relace v okamžiku, kdy uživatel zavře okno terminálu nebo kartu prohlížeče.
            }
            catch (Exception ex)
            {
                // Neočekávaná chyba (např. výpadek sítě nebo odmítnutí autentizace) – zalogujeme a vypíšeme červeně do terminálu.
                Logger.LogError($"NodeConsoleProxy - Console session error for server {server.Id}, lab {labId}, node {nodeId}: {ex.Message}");
                if (browserWs.State == WebSocketState.Open)
                {
                    await SendTerminalMessageAsync(
                        browserWs,
                        $"\r\n\x1b[31m[NetResVM] Connection failed: {ex.Message}\x1b[0m\r\n",
                        CancellationToken.None);
                }
            }
            finally
            {
                // Korektní ukončení WebSocket handshake (odeslání Close rámce), pokud je spojení stále otevřené.
                if (browserWs.State == WebSocketState.Open || browserWs.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        await browserWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignorujeme chyby při zavírání, pokud už klient spojení mezitím sám ukončil.
                    }
                }
            }
        }

        /// <summary>
        /// Odešle textovou stavovou zprávu (podporující barvy pomocí ANSI escape sekvencí) do terminálu <c>xterm.js</c> v prohlížeči.
        /// </summary>
        /// <param name="ws">Otevřený WebSocket směrem k prohlížeči.</param>
        /// <param name="message">Text zprávy včetně případných ANSI sekvencí a odřádkování <c>\r\n</c>.</param>
        /// <param name="cancellationToken">Token pro zrušení asynchronní operace.</param>
        private static async Task SendTerminalMessageAsync(WebSocket ws, string message, CancellationToken cancellationToken)
        {
            if (ws.State != WebSocketState.Open)
                return;
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
        }
    }
}
