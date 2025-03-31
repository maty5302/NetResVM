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
