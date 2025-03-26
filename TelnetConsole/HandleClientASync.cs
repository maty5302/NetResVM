using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetConsole
{
    internal static class HandleClientASync
    {
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

        static string ExecuteCommand (string command)
        {
            
            
            string[] args = command.Split(" ") ;
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    if (args[0] == "list")
                    {
                        if (args[1]=="servers")
                        {
                            return Command.ListServers();
                        }
                        else if(args[1]=="labs")
                        {
                            return Command.ListLabs(args[2]);
                        }
                        else if (args[1]=="users")
                        {
                            return Command.ListUsers();
                        }
                        else
                            return $"Unknown argument {args[1]}";
                    }
                    else if(args[0] == "start")
                    {
                        if(!String.IsNullOrEmpty(args[1]) && !String.IsNullOrEmpty(args[2]))
                            return Command.StartLab(args[1], args[2]);
                        return GetHelp();
                    }
                    else if (args[0] == "stop")
                    {

                    }
                    else if (args[0] == "deactivate")
                    {

                    }
                    else if (args[0] == "activate")
                    {

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
