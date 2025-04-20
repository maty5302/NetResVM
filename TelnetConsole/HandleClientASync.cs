using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetConsole
{
    /// <summary>
    /// Class that handles the Telnet client connection.
    /// </summary>
    internal static class HandleClientASync
    {
        /// <summary>
        /// Handles the client connection.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> representing the connected client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sets up stream readers and writers using ASCII encoding to communicate with the client.
        /// It enters a loop where it reads input line-by-line:
        /// - Terminates the connection if the input is null or "exit".
        /// - Responds with help text if the input is "help".
        /// - Otherwise, it executes the command using <c>ExecuteCommand</c> and returns the response.
        /// All resources are properly disposed using <c>using</c> statements.
        /// </remarks>
        public static async Task Handle(TcpClient client)
        {
            using(client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII))
            using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                while (true)
                {
                    var command = await reader.ReadLineAsync();
                    if (command == null) break;
                    if (command.ToLower() == "exit") break;
                    if(command.ToLower()=="help")
                    {
                        await writer.WriteLineAsync(GetHelp());
                        continue;
                    }

                    string response = ExecuteCommand(command);
                    await writer.WriteLineAsync(response);
                }
            }

        }

        /// <summary>
        /// Executes the command received from the client.
        /// </summary>
        /// <param name="command">The command string sent by the client.</param>
        /// <returns>
        /// A response string based on the result of the executed command, or a help message if the command is invalid.
        /// </returns>
        static string ExecuteCommand (string command)
        {
            
            
            string[] args = command.Split(" ") ;
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    if (args[0] == "list")
                    {
                        if (args[1] == "servers")
                        {
                            return Command.ListServers();
                        }
                        else if (args[1] == "labs" && args.Length == 3)
                        {
                            if (!String.IsNullOrEmpty(args[2]))
                                return Command.ListLabs(args[2]);
                            return GetHelp();
                        }
                        else if (args[1] == "users")
                        {
                            return Command.ListUsers();
                        }
                        else
                            return GetHelp();
                    }
                    else if(args[0] == "start" && args.Length == 3)
                    {
                        if(!String.IsNullOrEmpty(args[1]) && !String.IsNullOrEmpty(args[2]))
                            return Command.StartLab(args[1], args[2]);
                        return GetHelp();
                    }
                    else if (args[0] == "stop" && args.Length == 3)
                    {
                        if (!String.IsNullOrEmpty(args[1]) && !String.IsNullOrEmpty(args[2]))
                            return Command.StopLab(args[1], args[2]);
                        return GetHelp();
                    }
                    else if (args[0] == "deactivate")
                    {
                        if(!String.IsNullOrEmpty(args[1]))
                        {
                            int id = -1;
                            int.TryParse(args[1],out id);
                            if (id == -1 || id==0)
                                return GetHelp();
                            return Command.DeactivateUser(id);
                        }
                        return GetHelp();

                    }
                    else if (args[0] == "activate")
                    {
                        if (!String.IsNullOrEmpty(args[1]))
                        {
                            int id = -1;
                            int.TryParse(args[1], out id);
                            if (id == -1 || id == 0)
                                return GetHelp();
                            return Command.ActivateUser(id);
                        }
                        return GetHelp();
                    }
                    else
                        return $"Unknown command {command}";
                }
                else if (args[0] == "")
                    return "";
                else
                    return $"Unknown command {command}";
            }
            return "";
        }

        /// <summary>
        /// Generates the help text for the Telnet console commands.
        /// </summary>
        /// <returns> returns generated help for console to show </returns>
        private static string GetHelp()
        {
            //sb.AppendLine($"{"".PadRight(commandWidth)} | {"".PadRight(descriptionWidth)}");

            int commandWidth = 25, descriptionWidth = 50;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{"Command".PadRight(commandWidth)} | {"Description".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"list servers".PadRight(commandWidth)} | {"Shows a list of available servers".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"list labs {serverID}".PadRight(commandWidth)} | {"Shows a list of labs on specified server".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"list users".PadRight(commandWidth)} | {"Shows all registered users".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"start {serverID} {labID}".PadRight(commandWidth)} | {"Start lab on specified server".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"stop {serverID} {labID}".PadRight(commandWidth)} | {"Stop lab on specified server".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"activate {userID}".PadRight(commandWidth)} | {"Activate user by its ID".PadRight(descriptionWidth)}");
            sb.AppendLine($"{"deactivate {userID}".PadRight(commandWidth)} | {"Deactivate user by its ID".PadRight(descriptionWidth)}");

            return sb.ToString();
        }
    }
}
