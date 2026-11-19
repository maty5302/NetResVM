using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace ApiEVE.Client
{
    /// <summary>
    /// Poskytuje klientskou implementaci pro navázání přímého TCP/Telnet spojení s konzolovým portem
    /// virtuálního uzlu na platformě EVE-NG a jeho obousměrné přemostění do WebSocketu prohlížeče (<c>xterm.js</c>).
    /// </summary>
    /// <remarks>
    /// Platforma EVE-NG při spuštění uzlu vystaví jeho sériovou konzoli na dynamicky přiděleném TCP portu
    /// (typicky v rozsahu 32769+), přičemž tento port vrací v metadatech uzlu pod klíčem <c>UrlConnect</c>
    /// (např. <c>telnet://158.196.x.x:32769</c>). Tato třída zajišťuje TCP spojení, vyjednání Telnet voleb (RFC 854)
    /// a obousměrný přenos dat mezi <see cref="NetworkStream"/> a <see cref="WebSocket"/>.
    /// </remarks>
    public class ApiEVEConsole
    {
        // Konstanty protokolu Telnet (RFC 854) pro filtraci a vyjednávání řídicích sekvencí IAC (Interpret As Command)
        private const byte TelnetIac = 0xFF;
        private const byte TelnetSe = 0xF0;
        private const byte TelnetSb = 0xFA;
        private const byte TelnetWill = 0xFB;
        private const byte TelnetWont = 0xFC;
        private const byte TelnetDo = 0xFD;
        private const byte TelnetDont = 0xFE;

        private const byte TelnetOptEcho = 0x01;
        private const byte TelnetOptSuppressGoAhead = 0x03;

        /// <summary>
        /// Extrahuje samotnou IP adresu nebo hostname ze zadané URL adresy serveru
        /// (odstraní schéma <c>http://</c> či <c>https://</c>, případné číslo portu a lomítka).
        /// </summary>
        /// <param name="rawUrl">Uložená adresa serveru z databáze (např. <c>http://158.196.10.20</c> nebo <c>158.196.10.20</c>).</param>
        /// <returns>Čistý hostname nebo IP adresa vhodná pro navázání TCP/Telnet spojení.</returns>
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
        /// Pokusí se bezpečně zparsovat číslo TCP portu z připojovacího URI řetězce (např. <c>telnet://10.0.0.1:32769</c>).
        /// </summary>
        /// <param name="urlConnect">Řetězec s adresou a portem získaný z metadat EVE-NG uzlu.</param>
        /// <param name="port">Výstupní parametr obsahující zjištěné číslo portu (1–65535).</param>
        /// <returns><see langword="true"/>, pokud se port podařilo úspěšně přečíst; jinak <see langword="false"/>.</returns>
        public static bool TryParseTelnetPort(string? urlConnect, out int port)
        {
            port = 0;
            if (string.IsNullOrWhiteSpace(urlConnect))
                return false;

            string trimmed = urlConnect.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                // Ignorujeme standardní HTTP/HTTPS schémata, pokud jde o webové HTML5 URL namísto telnet:// portu
                if (string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (uri.Port > 0 && uri.Port <= 65535)
                {
                    port = uri.Port;
                    return true;
                }
            }

            int colonIndex = trimmed.LastIndexOf(':');
            if (colonIndex >= 0 && colonIndex + 1 < trimmed.Length)
            {
                string portPart = new string(trimmed[(colonIndex + 1)..].TakeWhile(char.IsDigit).ToArray());
                return int.TryParse(portPart, out port) && port > 0 && port <= 65535;
            }

            return false;
        }

        /// <summary>
        /// Zpracuje surový proud bajtů z Telnet serveru EVE-NG, odfiltruje řídicí sekvence Telnet IAC (<c>0xFF</c>),
        /// aby se v terminálu <c>xterm.js</c> nezobrazovaly nečitelné znaky, a sestaví odpověď pro Telnet vyjednávání.
        /// </summary>
        /// <param name="input">Vstupní buffer přečtený ze síťového proudu (<see cref="NetworkStream"/>).</param>
        /// <param name="count">Počet platných bajtů ve vstupním bufferu.</param>
        /// <param name="displayBytes">Výstupní pole čistých terminálových dat určených k odeslání do <c>xterm.js</c>.</param>
        /// <param name="negotiationReply">Výstupní pole obsahující odpověď Telnet vyjednávání (IAC DO/DONT/WILL/WONT) pro server.</param>
        public static void ProcessTelnetBuffer(
            byte[] input,
            int count,
            out byte[] displayBytes,
            out byte[] negotiationReply)
        {
            var cleanOutput = new List<byte>(count);
            var replyOutput = new List<byte>();

            int i = 0;
            while (i < count)
            {
                byte current = input[i];
                if (current != TelnetIac)
                {
                    cleanOutput.Add(current);
                    i++;
                    continue;
                }

                // Samotný bajt IAC na konci bufferu
                if (i + 1 >= count)
                {
                    i++;
                    break;
                }

                byte command = input[i + 1];

                // Dvojitý IAC (0xFF 0xFF) představuje datový bajt 0xFF
                if (command == TelnetIac)
                {
                    cleanOutput.Add(TelnetIac);
                    i += 2;
                    continue;
                }

                // Subnegotiation: IAC SB ... IAC SE
                if (command == TelnetSb)
                {
                    i += 2;
                    while (i < count)
                    {
                        if (input[i] == TelnetIac && i + 1 < count && input[i + 1] == TelnetSe)
                        {
                            i += 2;
                            break;
                        }
                        i++;
                    }
                    continue;
                }

                // Tříbajtové příkazy vyjednávání voleb: WILL, WONT, DO, DONT
                if (command is TelnetWill or TelnetWont or TelnetDo or TelnetDont)
                {
                    if (i + 2 >= count)
                    {
                        i = count;
                        break;
                    }

                    byte option = input[i + 2];
                    if (command == TelnetWill)
                    {
                        // Povolíme serverový ECHO a SUPPRESS-GO-AHEAD pro interaktivní režim znak po znaku
                        byte responseCmd = (option == TelnetOptEcho || option == TelnetOptSuppressGoAhead)
                            ? TelnetDo
                            : TelnetDont;
                        replyOutput.Add(TelnetIac);
                        replyOutput.Add(responseCmd);
                        replyOutput.Add(option);
                    }
                    else if (command == TelnetDo)
                    {
                        byte responseCmd = option == TelnetOptSuppressGoAhead
                            ? TelnetWill
                            : TelnetWont;
                        replyOutput.Add(TelnetIac);
                        replyOutput.Add(responseCmd);
                        replyOutput.Add(option);
                    }
                    else if (command == TelnetWont)
                    {
                        replyOutput.Add(TelnetIac);
                        replyOutput.Add(TelnetDont);
                        replyOutput.Add(option);
                    }
                    else if (command == TelnetDont)
                    {
                        replyOutput.Add(TelnetIac);
                        replyOutput.Add(TelnetWont);
                        replyOutput.Add(option);
                    }

                    i += 3;
                    continue;
                }

                // Ostatní dvoubajtové Telnet příkazy (NOP, BRK, IP, AO, AYT, EC, EL, GA)
                i += 2;
            }

            displayBytes = cleanOutput.ToArray();
            negotiationReply = replyOutput.ToArray();
        }

        /// <summary>
        /// Naváže přímé TCP/Telnet spojení s konzolovým portem virtuálního uzlu na platformě EVE-NG
        /// a obousměrně propojí síťový proud (<see cref="NetworkStream"/>) s WebSocketem prohlížeče.
        /// </summary>
        /// <param name="rawServerUrl">Adresa EVE-NG serveru z databáze.</param>
        /// <param name="nodeName">Zobrazovaný název uzlu pro informační výpis v terminálu.</param>
        /// <param name="urlConnect">Připojovací řetězec z metadat uzlu (např. <c>telnet://158.196.x.x:32769</c>).</param>
        /// <param name="browserWs">Aktivní WebSocket spojení do prohlížeče (<c>xterm.js</c>).</param>
        /// <param name="cancellationToken">Token signalizující ukončení spojení.</param>
        public async Task ConnectConsoleAsync(
            string rawServerUrl,
            string nodeName,
            string? urlConnect,
            WebSocket browserWs,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(urlConnect))
            {
                await SendTerminalMessageAsync(
                    browserWs,
                    "\x1b[31m[NetResVM] Could not resolve Telnet port for this EVE-NG node.\x1b[0m\r\n",
                    cancellationToken);
                return;
            }

            // Extrakce čísla TCP portu z řetězce "telnet://<ip>:<port>"
            if (!TryParseTelnetPort(urlConnect, out int port))
            {
                await SendTerminalMessageAsync(
                    browserWs,
                    $"\x1b[31m[NetResVM] Unsupported console URL format: {urlConnect}\x1b[0m\r\n",
                    cancellationToken);
                return;
            }

            string host = ExtractHost(rawServerUrl);

            await SendTerminalMessageAsync(
                browserWs,
                $"\x1b[36m[NetResVM] Connecting to EVE-NG node '{nodeName}' ({host}:{port})...\x1b[0m\r\n",
                cancellationToken);

            using var tcpClient = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(10));
            await tcpClient.ConnectAsync(host, port, connectCts.Token);
            using var netStream = tcpClient.GetStream();

            await SendTerminalMessageAsync(
                browserWs,
                "\x1b[32m[NetResVM] Connected.\x1b[0m\r\n",
                cancellationToken);

            // Probuzení konzolového promptu odesláním znaku CR (\r)
            await netStream.WriteAsync(new byte[] { (byte)'\r' }, 0, 1, cancellationToken);
            await netStream.FlushAsync(cancellationToken);

            using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Smyčka čtoucí data z TCP socketu EVE-NG, filtrující Telnet IAC sekvence a odesílající čistý výstup do WebSocketu prohlížeče
            var readTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && browserWs.State == WebSocketState.Open)
                    {
                        int bytesRead = await netStream.ReadAsync(buffer, 0, buffer.Length, sessionCts.Token);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        ProcessTelnetBuffer(buffer, bytesRead, out byte[] displayBytes, out byte[] negotiationReply);

                        if (negotiationReply.Length > 0)
                        {
                            await netStream.WriteAsync(negotiationReply, 0, negotiationReply.Length, sessionCts.Token);
                            await netStream.FlushAsync(sessionCts.Token);
                        }

                        if (displayBytes.Length > 0)
                        {
                            await browserWs.SendAsync(
                                new ArraySegment<byte>(displayBytes, 0, displayBytes.Length),
                                WebSocketMessageType.Binary,
                                endOfMessage: true,
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

            // Smyčka čtoucí stisky kláves z WebSocketu prohlížeče a zapisující je do TCP socketu EVE-NG
            var writeTask = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                try
                {
                    while (!sessionCts.Token.IsCancellationRequested && browserWs.State == WebSocketState.Open)
                    {
                        var result = await browserWs.ReceiveAsync(new ArraySegment<byte>(buffer), sessionCts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        if (result.Count > 0)
                        {
                            await netStream.WriteAsync(buffer, 0, result.Count, sessionCts.Token);
                            await netStream.FlushAsync(sessionCts.Token);
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

            var completed = await Task.WhenAny(readTask, writeTask);
            sessionCts.Cancel();
            await completed;
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
