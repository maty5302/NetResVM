using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Common;
using SimpleLogger;

namespace ApiCisco.Client
{
    /// <summary>
    /// Poskytuje klientskou implementaci pro navázání interaktivní sériové konzole (CLI) k síťovému prvku
    /// na platformě Cisco Modeling Labs (CML) a její obousměrné přemostění do WebSocketu prohlížeče (<c>xterm.js</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Třída podporuje dva komunikační režimy vůči serveru Cisco CML:
    /// <list type="bullet">
    ///   <item>
    ///     <description><strong>Primární režim (SSH Terminal Server, port 22):</strong> Využívá vestavěný konzolový
    ///     multiplexer CML (<c>virl2-terminal-server</c> / <c>CML2 Console Server</c>), kde po navázání SSH relace
    ///     a otevření pseudo-terminálu (PTY s emulací <c>xterm-256color</c>) automaticky odešle příkaz
    ///     <c>open /{labId}/{nodeId}/{line}</c> pro přepnutí do sériové linky daného routeru nebo switche.</description>
    ///   </item>
    ///   <item>
    ///     <description><strong>Záložní režim (HTTPS/WSS Dispatcher, port 443):</strong> Pokud je port 22 blokován
    ///     firewallem, klient automaticky získá UUID konzolové linky přes REST API (<see cref="ApiCiscoNode.GetConsoleKey"/>)
    ///     a naváže interní CML WebSocket spojení na <c>wss://&lt;host&gt;/ws/dispatch/frontend/console?uuid=&lt;uuid&gt;</c>.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ApiCiscoConsole
    {
        /// <summary>
        /// Regulární výraz pro striktní validaci identifikátorů laboratoře (<c>labId</c>) a uzlu (<c>nodeId</c>).
        /// Zabraňuje útokům typu Command Injection při skládání příkazu <c>open /{labId}/{nodeId}/{line}</c>
        /// do konzolového shellu CML. Povoluje pouze alfanumerické znaky, pomlčku, podtržítko a tečku.
        /// </summary>
        private static readonly Regex SafeIdentifierRegex = new("^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);

        /// <summary>
        /// Instance loggeru pro zaznamenávání chyb a varování během životního cyklu konzolové relace.
        /// </summary>
        private readonly ILogger _logger = FileLogger.Instance;

        private readonly ApiCiscoNode _nodeClient;

        /// <summary>
        /// Inicializuje novou instanci třídy <see cref="ApiCiscoConsole"/> s výchozím klientem <see cref="ApiCiscoNode"/>.
        /// </summary>
        public ApiCiscoConsole() : this(new ApiCiscoNode())
        {
        }

        /// <summary>
        /// Inicializuje novou instanci třídy <see cref="ApiCiscoConsole"/> se zadaným klientem <see cref="ApiCiscoNode"/>.
        /// </summary>
        /// <param name="nodeClient">Klient pro dotazování informací a konzolových klíčů uzlů v CML REST API.</param>
        public ApiCiscoConsole(ApiCiscoNode nodeClient)
        {
            _nodeClient = nodeClient;
        }

        /// <summary>
        /// Ověří bezpečnost identifikátorů (<paramref name="labId"/>, <paramref name="nodeId"/> a <paramref name="line"/>)
        /// proti podvržení příkazů (Command Injection) do konzolového multiplexeru CML.
        /// </summary>
        /// <param name="labId">Identifikátor laboratoře.</param>
        /// <param name="nodeId">Identifikátor uzlu.</param>
        /// <param name="line">Index sériové konzolové linky (0–15).</param>
        /// <returns><see langword="true"/>, pokud jsou všechny parametry bezpečné a platné; jinak <see langword="false"/>.</returns>
        public static bool IsValidTarget(string labId, string nodeId, int line)
        {
            return !string.IsNullOrWhiteSpace(labId) &&
                   !string.IsNullOrWhiteSpace(nodeId) &&
                   SafeIdentifierRegex.IsMatch(labId) &&
                   SafeIdentifierRegex.IsMatch(nodeId) &&
                   line >= 0 &&
                   line <= 15;
        }

        /// <summary>
        /// Extrahuje samotnou IP adresu nebo hostname ze zadané URL adresy serveru
        /// (odstraní schéma <c>http://</c> či <c>https://</c>, případné číslo portu a lomítka).
        /// </summary>
        /// <param name="rawUrl">Uložená adresa serveru z databáze (např. <c>https://158.196.10.20/api/v0/</c> nebo <c>158.196.10.20</c>).</param>
        /// <returns>Čistý hostname nebo IP adresa vhodná pro navázání SSH/WSS spojení.</returns>
        public static string ExtractHost(string rawUrl)
        {
            string trimmed = rawUrl.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                {
                    return uri.Host;
                }
            }

            return trimmed.Split('/')[0].Split(':')[0];
        }

        /// <summary>
        /// Naváže interaktivní konzolovou relaci s prvkem v Cisco CML přes SSH Terminal Server (port 22)
        /// s automatickým přechodem (fallbackem) na CML HTTPS/WSS WebSocket Dispatcher (port 443), pokud není port 22 dostupný.
        /// </summary>
        /// <param name="httpClient">Autentizovaný HTTP klient pro komunikaci s REST API Cisco CML.</param>
        /// <param name="rawServerUrl">Adresa serveru z databáze.</param>
        /// <param name="username">Uživatelské jméno pro autentizaci na CML serveru.</param>
        /// <param name="password">Heslo pro autentizaci na CML serveru.</param>
        /// <param name="labId">Unikátní identifikátor laboratoře v CML.</param>
        /// <param name="nodeId">Unikátní identifikátor uzlu v laboratoři.</param>
        /// <param name="browserWs">Aktivní WebSocket spojení do prohlížeče (<c>xterm.js</c>).</param>
        /// <param name="line">Číslo sériové konzolové linky prvku (výchozí je <c>0</c>).</param>
        /// <param name="cols">Počet sloupců terminálu (<c>xterm.js</c>).</param>
        /// <param name="rows">Počet řádků terminálu (<c>xterm.js</c>).</param>
        /// <param name="cancellationToken">Token signalizující ukončení spojení.</param>
        public async Task ConnectConsoleAsync(
            ApiCiscoHttpClient httpClient,
            string rawServerUrl,
            string username,
            string password,
            string labId,
            string nodeId,
            WebSocket browserWs,
            int line = 0,
            uint cols = 120,
            uint rows = 32,
            CancellationToken cancellationToken = default)
        {
            // BEZPEČNOSTNÍ KONTROLA: Ověření, že labId a nodeId neobsahují řídicí znaky (např. \n, ;, |),
            // kterými by bylo možné podvrhnout jiný příkaz do CML konzolového multiplexeru.
            if (!IsValidTarget(labId, nodeId, line))
            {
                await SendTerminalMessageAsync(
                    browserWs,
                    "\x1b[31m[NetResVM] Invalid lab or node identifier.\x1b[0m\r\n",
                    cancellationToken);
                return;
            }

            string host = ExtractHost(rawServerUrl);

            // Informování uživatele přímo v okně terminálu (pomocí ANSI escape sekvencí pro azurovou barvu \x1b[36m)
            await SendTerminalMessageAsync(
                browserWs,
                $"\x1b[36m[NetResVM] Connecting to CML node '{nodeId}' in lab '{labId}' ({host})...\x1b[0m\r\n",
                cancellationToken);

            // 1. PRIMÁRNÍ STRATEGIE PRO CML:
            // Připojení přes vestavěný CML SSH Terminal Server na portu 22 a odeslání příkazu "open /{labId}/{nodeId}/{line}".
            try
            {
                await ConnectSshConsoleAsync(
                    browserWs,
                    host,
                    username,
                    password,
                    labId,
                    nodeId,
                    line,
                    cols,
                    rows,
                    cancellationToken);
                return;
            }
            catch (Exception sshEx) when (!cancellationToken.IsCancellationRequested && browserWs.State == WebSocketState.Open)
            {
                // 2. ZÁLOŽNÍ (FALLBACK) STRATEGIE PRO CML:
                // Pokud není SSH port 22 dostupný (např. kvůli firewallu mezi aplikačním serverem a CML),
                // zaznamenáme varování a automaticky zkusíme komunikaci přes HTTPS REST API a WSS WebSocket dispatcher (port 443).
                _logger.LogWarning($"ApiCiscoConsole - CML SSH port 22 failed for {host} ({sshEx.Message}), attempting HTTPS WebSocket dispatcher fallback...");
                await SendTerminalMessageAsync(
                    browserWs,
                    "\x1b[33m[NetResVM] SSH port 22 unavailable, trying CML WebSocket API fallback...\x1b[0m\r\n",
                    cancellationToken);

                await ConnectWebSocketDispatcherAsync(
                    httpClient,
                    browserWs,
                    host,
                    labId,
                    nodeId,
                    line,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Naváže SSH spojení s vestavěným konzolovým serverem Cisco CML (port 22), alokuje pseudo-terminál (PTY),
        /// automaticky otevře sériovou linku požadovaného prvku příkazem <c>open /{labId}/{nodeId}/{line}</c>
        /// a následně obousměrně (full-duplex) přeposílá binární proud dat mezi SSH kanálem a WebSocketem prohlížeče.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Technické detaily implementace:</strong>
        /// <list type="bullet">
        ///   <item>
        ///     <description><strong>Duální SSH autentizace:</strong> Některé verze CML vyžadují standardní <c>password</c>
        ///     autentizaci, zatímco jiné využívají PAM modul přes <c>keyboard-interactive</c>. Metoda proto registruje
        ///     oba mechanismy současně (<see cref="PasswordAuthenticationMethod"/> i <see cref="KeyboardInteractiveAuthenticationMethod"/>).</description>
        ///   </item>
        ///   <item>
        ///     <description><strong>Synchronizace automatického příkazu <c>open</c>:</strong> Po otevření shellu vypisuje CML
        ///     uvítací banner zakončený promptem <c>consoles&gt;</c>. Čtecí vlákno sleduje příchozí text a jakmile detekuje
        ///     řetězec <c>consoles&gt;</c>, odešle příkaz <c>open</c>. Pro případ, že by byl banner upraven, běží paralelně
        ///     časovač (1,2 s), přičemž atomická operace <see cref="Interlocked.CompareExchange(ref int, int, int)"/> zaručuje,
        ///     že se příkaz <c>open</c> odešle právě jednou (Thread-Safe One-Shot Execution).</description>
        ///   </item>
        /// </list>
        /// </para>
        /// </remarks>
        private static async Task ConnectSshConsoleAsync(
            WebSocket browserWs,
            string host,
            string username,
            string password,
            string labId,
            string nodeId,
            int line,
            uint cols,
            uint rows,
            CancellationToken cancellationToken)
        {
            // Nastavení podpory pro keyboard-interactive autentizaci (odpovídá heslem na případné výzvy PAM modulu)
            var keyboardAuth = new KeyboardInteractiveAuthenticationMethod(username);
            keyboardAuth.AuthenticationPrompt += (_, e) =>
            {
                foreach (AuthenticationPrompt prompt in e.Prompts)
                {
                    prompt.Response = password;
                }
            };

            // Nastavení standardní password autentizace a vytvoření konfiguračního objektu SSH spojení na port 22
            var passwordAuth = new PasswordAuthenticationMethod(username, password);
            var connectionInfo = new ConnectionInfo(host, 22, username, passwordAuth, keyboardAuth)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            using var sshClient = new SshClient(connectionInfo);
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(12));
            await sshClient.ConnectAsync(connectCts.Token);

            // Omezení rozměrů terminálu do rozumných mezí (ochrana proti neplatným hodnotám z query stringu)
            uint termCols = cols >= 20 && cols <= 500 ? cols : 120;
            uint termRows = rows >= 10 && rows <= 200 ? rows : 32;

            // Vytvoření interaktivního PTY (pseudo-terminálu) s emulací xterm-256color, aby fungovalo zvýrazňování,
            // mazání řádků, šipky v historii příkazů a tab-completion v Cisco IOS.
            using var shellStream = sshClient.CreateShellStream("xterm-256color", termCols, termRows, 800, 600, 8192);

            // Příkaz pro CML Terminal Server (prompt_toolkit v PTY režimu očekává ukončení znakem CR \r),
            // který přepne relaci z menu "consoles>" přímo na sériový port prvku
            string openCommand = $"open /{labId}/{nodeId}/{line}\r";

            // Příznak (0 = neodesláno, 1 = odesláno) řízený atomicky přes třídu Interlocked,
            // aby se příkaz "open" neodeslal dvakrát při souběhu detekce promptu a záložního časovače.
            int openSent = 0;

            // Sdružený CancellationTokenSource – jakmile skončí čtecí nebo zapisovací smyčka (např. zavřením okna),
            // okamžitě se zruší i druhá smyčka a uvolní se SSH spojení.
            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Záložní úloha (Fallback Timer): Pokud by se do 1,2 sekundy v příchozích datech neobjevil prompt "consoles>",
            // odešle příkaz "open" automaticky. Po dalších 400 ms pošle znak CR (\r), aby router vypsal svůj CLI prompt (např. "Router#").
            var autoOpenTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1200, sessionCts.Token);
                    if (Interlocked.CompareExchange(ref openSent, 1, 0) == 0)
                    {
                        byte[] cmdBytes = Encoding.UTF8.GetBytes(openCommand);
                        await shellStream.WriteAsync(cmdBytes, 0, cmdBytes.Length, sessionCts.Token);
                        await shellStream.FlushAsync(sessionCts.Token);

                        await Task.Delay(400, sessionCts.Token);
                        await shellStream.WriteAsync(new byte[] { (byte)'\r' }, 0, 1, sessionCts.Token);
                        await shellStream.FlushAsync(sessionCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, sessionCts.Token);

            // SMYČKA 1 (Downstream): Čtení výstupu ze vzdáleného SSH PTY streamu (CML) a jeho odesílání do prohlížeče (WebSocket)
            var readFromSshTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                var initialBannerBuilder = new StringBuilder();

                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && browserWs.State == WebSocketState.Open && sshClient.IsConnected)
                    {
                        int bytesRead = await shellStream.ReadAsync(buffer, 0, buffer.Length, sessionCts.Token);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        // Přeposlání surových bajtů (včetně ANSI řídicích sekvencí) přímo do xterm.js v prohlížeči
                        await browserWs.SendAsync(
                            new ArraySegment<byte>(buffer, 0, bytesRead),
                            WebSocketMessageType.Binary,
                            endOfMessage: true,
                            sessionCts.Token);

                        // Pokud jsme ještě neodeslali příkaz "open", sledujeme, zda CML nevypsalo úvodní prompt "consoles>"
                        if (Volatile.Read(ref openSent) == 0)
                        {
                            initialBannerBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                            if (initialBannerBuilder.ToString().Contains("consoles>", StringComparison.OrdinalIgnoreCase))
                            {
                                // Atomicky přepneme openSent z 0 na 1; pokud jsme první, odešleme příkaz "open /{labId}/{nodeId}/{line}\r"
                                if (Interlocked.CompareExchange(ref openSent, 1, 0) == 0)
                                {
                                    byte[] cmdBytes = Encoding.UTF8.GetBytes(openCommand);
                                    await shellStream.WriteAsync(cmdBytes, 0, cmdBytes.Length, sessionCts.Token);
                                    await shellStream.FlushAsync(sessionCts.Token);

                                    // Krátce po připojení na sériovou linku pošleme Enter (\r), aby se probudil prompt Cisco IOS
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await Task.Delay(400, sessionCts.Token);
                                            await shellStream.WriteAsync(new byte[] { (byte)'\r' }, 0, 1, sessionCts.Token);
                                            await shellStream.FlushAsync(sessionCts.Token);
                                        }
                                        catch
                                        {
                                        }
                                    }, sessionCts.Token);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    sessionCts.Cancel();
                }
            }, sessionCts.Token);

            // SMYČKA 2 (Upstream): Čtení stisků kláves z prohlížeče (WebSocket) a jejich zápis do SSH PTY streamu (CML)
            var writeToSshTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && browserWs.State == WebSocketState.Open && sshClient.IsConnected)
                    {
                        var result = await browserWs.ReceiveAsync(new ArraySegment<byte>(buffer), sessionCts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }

                        if (result.Count > 0)
                        {
                            await shellStream.WriteAsync(buffer, 0, result.Count, sessionCts.Token);
                            await shellStream.FlushAsync(sessionCts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    sessionCts.Cancel();
                }
            }, sessionCts.Token);

            // Čekáme, dokud neskončí alespoň jedna ze smyček (např. uživatel zavře okno nebo prvek ukončí relaci)
            await Task.WhenAny(readFromSshTask, writeToSshTask);
            sessionCts.Cancel();

            if (sshClient.IsConnected)
            {
                sshClient.Disconnect();
            }
        }

        /// <summary>
        /// Záložní (fallback) metoda pro připojení ke konzoli prvku v Cisco CML přes port 443 (HTTPS/WSS),
        /// která se použije v případě, že je SSH port 22 na cestě k CML serveru filtrován firewallem.
        /// </summary>
        /// <remarks>
        /// Postup komunikace odpovídá internímu protokolu webového rozhraní CML:
        /// <list type="number">
        ///   <item><description>Využití JWT Bearer tokenu z autentizovaného <see cref="ApiCiscoHttpClient"/>.</description></item>
        ///   <item><description>Získání unikátního klíče (UUID) sériové konzole přes <see cref="ApiCiscoNode.GetConsoleKey"/>.</description></item>
        ///   <item><description>Navázání WebSocket spojení na <c>wss://&lt;host&gt;/ws/dispatch/frontend/console?uuid=&lt;consoleUuid&gt;</c>
        ///   a obousměrné přemosťování rámců mezi prohlížečem a CML.</description></item>
        /// </list>
        /// </remarks>
        private async Task ConnectWebSocketDispatcherAsync(
            ApiCiscoHttpClient httpClient,
            WebSocket browserWs,
            string host,
            string labId,
            string nodeId,
            int line,
            CancellationToken cancellationToken)
        {
            // Krok 1: Získání aktivního JWT Bearer tokenu z autentizovaného HTTP klienta
            string? token = httpClient.Client.DefaultRequestHeaders.Authorization?.Parameter?.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Missing Cisco CML authentication token.");
            }

            // Krok 2: Dotaz na REST API pro zjištění UUID konzolové relace daného prvku
            string? consoleUuid = await _nodeClient.GetConsoleKey(httpClient, labId, nodeId, line, cancellationToken);
            if (string.IsNullOrWhiteSpace(consoleUuid))
            {
                consoleUuid = nodeId;
            }

            // Krok 3: Otevření klientského WebSocketu vůči CML dispatcheru (s ignorováním self-signed certifikátů)
            using var cmlWs = new ClientWebSocket();
            cmlWs.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            cmlWs.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            cmlWs.Options.SetRequestHeader("Cookie", $"token={token}");

            var wsUri = new Uri($"wss://{host}/ws/dispatch/frontend/console?uuid={Uri.EscapeDataString(consoleUuid)}");
            await cmlWs.ConnectAsync(wsUri, cancellationToken);

            await SendTerminalMessageAsync(
                browserWs,
                "\x1b[32m[NetResVM] Connected via CML WebSocket dispatcher.\x1b[0m\r\n",
                cancellationToken);

            // Odeslání znaku CR (\r) pro okamžité vykreslení promptu prvku
            await cmlWs.SendAsync(
                new ArraySegment<byte>(new byte[] { (byte)'\r' }),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);

            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Smyčka přeposílající výstup z CML WebSocketu do prohlížeče
            var cmlToBrowserTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && cmlWs.State == WebSocketState.Open && browserWs.State == WebSocketState.Open)
                    {
                        var result = await cmlWs.ReceiveAsync(new ArraySegment<byte>(buffer), sessionCts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        if (result.Count > 0)
                        {
                            await browserWs.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                WebSocketMessageType.Binary,
                                result.EndOfMessage,
                                sessionCts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    sessionCts.Cancel();
                }
            }, sessionCts.Token);

            // Smyčka přeposílající vstupy uživatele z prohlížeče do CML WebSocketu
            var browserToCmlTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && cmlWs.State == WebSocketState.Open && browserWs.State == WebSocketState.Open)
                    {
                        var result = await browserWs.ReceiveAsync(new ArraySegment<byte>(buffer), sessionCts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        if (result.Count > 0)
                        {
                            await cmlWs.SendAsync(
                                new ArraySegment<byte>(buffer, 0, result.Count),
                                WebSocketMessageType.Text,
                                result.EndOfMessage,
                                sessionCts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    sessionCts.Cancel();
                }
            }, sessionCts.Token);

            await Task.WhenAny(cmlToBrowserTask, browserToCmlTask);
            sessionCts.Cancel();
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
