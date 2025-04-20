using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        private static int port = 2323;

        /// <summary>
        /// Starts the Telnet listener on the specified port.
        /// </summary>
        /// <returns></returns>
        public static async Task StartListener()
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Telnet is running on port {port}...");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientASync.Handle(client);
            }
        }
    }
}
