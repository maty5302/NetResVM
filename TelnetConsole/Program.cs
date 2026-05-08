using System.Net;
using System.Net.Sockets;

namespace TelnetConsole
{
    /// <summary>
    /// Class that handles the Telnet console.
    /// </summary>
    public class TelnetConsole
    {
        /// <summary>
        /// Port number for the Telnet server.
        /// </summary>
        private static readonly int Port = 2323;

        /// <summary>
        /// Starts the Telnet listener on the specified port.
        /// </summary>
        /// <returns></returns>
        public static async Task StartListener()
        {
            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Console.WriteLine($"Telnet is running on port {Port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientASync.Handle(client);
            }
        }
    }
}
