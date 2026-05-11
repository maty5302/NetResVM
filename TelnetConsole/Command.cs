using ApiCisco;
using ApiEVE;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using System.Text;

namespace TelnetConsole
{
    /// <summary>
    /// Class that handles commands for managing servers, labs, and users.
    /// </summary>
    internal static class Command
    {
        private static readonly ServerService Server = new ServerService();
        private static readonly PlatformManager PlatformManager;

        // Statický konstruktor - spustí se jen jednou při prvním použití třídy Command
        static Command()
        {
            // Jelikož nemáme DI kontejner, vytvoříme si adaptéry manuálně
            var adapters = new List<IVirtualizationAdapter>
            {
                new CiscoCmlAdapter(),
                new EveNGAdapter() 
            };

            PlatformManager = new PlatformManager(adapters);
        }
        /// <summary>
        /// Lists all servers in the system.
        /// </summary>
        /// <returns> A response string based on the result of the ListServers </returns>
        public static string ListServers()
        {
            var list = Server.GetAllServers();
            if (list == null)
                return "";

            int idWidth = 5, ipWidth = 15, typeWidth = 12;
            var sb = new StringBuilder();
            sb.AppendLine($"{"ID".PadRight(idWidth)} | {"IP Address".PadRight(ipWidth)} | {"Server Type".PadRight(typeWidth)}");
            foreach (var item in list)
            {
                sb.AppendLine($"{item.Id.ToString().PadRight(idWidth)} | {item.IpAddress.PadRight(ipWidth)} | {item.Platform.ToString().PadRight(typeWidth)}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Lists all labs on a specified server.
        /// </summary>
        /// <param name="serverId"> ID of server we want labs from </param>
        /// <returns>  A response string based on the result of the ListLabs</returns>
        public static async Task<string> ListLabs(string serverId)
        {
            if (!int.TryParse(serverId, out int id) || id < 0) return "Invalid ID";

            var s = Server.GetServerById(id);
            if (s == null) return "Server not found";
            if (s.Platform == PlatformType.Unknown) return "Unsupported platform type.";

            var sb = new StringBuilder();
            int idWidth = 40, nameWidth = 25, dateWidth = 12;
            sb.AppendLine($"{"ID".PadRight(idWidth)} | {"Name".PadRight(nameWidth)} | {"Last Modified".PadRight(dateWidth)}");

            try
            {
                IVirtualizationAdapter adapter = PlatformManager.GetAdapter(s.Platform);
                var authResult = await adapter.AuthenticateAsync(id);
                if (!authResult.Valid) return $"Auth failed: {authResult.Message}";

                var labsResult = await adapter.GetLabsAsync(id);
                if (labsResult.Labs == null) return labsResult.Message;

                foreach (var lab in labsResult.Labs)
                {
                    string modDate = "N/A";
                    if (lab.Metadata.TryGetValue("LastModified", out var m)) modDate = m;
                    sb.AppendLine($"{lab.Id.PadRight(idWidth)} | {lab.Name.PadRight(nameWidth)} | {modDate.PadRight(dateWidth)}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Lists all registered users in the system.
        /// </summary>
        /// <returns> A response string based on the result of the ListUsers</returns>
        public static string ListUsers()
        {
            int idWidth = 5, nameWidth = 15, activeWidth = 6;
            var sb = new StringBuilder();
            UserService userService = new UserService();

            var users = userService.GetAllUsersInfo();
            sb.AppendLine($"{"ID".PadRight(idWidth)} | {"Username".PadRight(nameWidth)} | {"Active".PadRight(activeWidth)}");
            
            foreach (var user in users)
            {
                sb.AppendLine($"{user.Id.ToString().PadRight(idWidth)} | {user.Username.PadRight(nameWidth)} | {user.Active.ToString().PadRight(activeWidth)}");
            }
            return sb.ToString();
        }



        public static async Task<string> StartLab(string serverId, string labId)
        {
            return await ExecuteLabActionAsync(serverId, labId, true);
        }

        public static async Task<string> StopLab(string serverId, string labId)
        {
            return await ExecuteLabActionAsync(serverId, labId, false);
        }

        private static async Task<string> ExecuteLabActionAsync(string serverId, string labId, bool start)
        {
            if (!int.TryParse(serverId, out int id) || id < 0) return "Invalid ID";

            var s = Server.GetServerById(id);
            if (s == null) return "Server not found";
            if (s.Platform == PlatformType.Unknown) return "Unsupported platform type.";

            try
            {
                IVirtualizationAdapter adapter = PlatformManager.GetAdapter(s.Platform);
                var authResult = await adapter.AuthenticateAsync(id);
                if (!authResult.Valid) return $"Auth failed: {authResult.Message}";

                // Předpokládám, že do IVirtualizationAdapter jsi přidal StartLabAsync a StopLabAsync!
                var result = start ? await adapter.StartLabAsync(id, labId) : await adapter.StopLabAsync(id, labId);

                return result.value ? $"Lab {(start ? "started" : "stopped")} successfully." : $"{result.message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        /// <summary>
        /// Deactivates a user by their ID.
        /// </summary>
        /// <param name="userId"> ID of user to deactivate</param>
        /// <returns> A string indicating whether the operation was successful or not</returns>
        public static string DeactivateUser(int userId)
        {
            UserService userService = new UserService();
            var res = userService.UpdateUser(userId, false);
            if (!res)
                return $"User with ID {userId} cannot be deactivated";
            return $"User with ID {userId} deactivated";
        }

        /// <summary>
        /// Activates a user by their ID.
        /// </summary>
        /// <param name="userId"> ID of user to deactivate</param>
        /// <returns> A string indicating whether the operation was successful or not</returns>
        public static string ActivateUser(int userId)
        {
            UserService userService = new UserService();
            var res = userService.UpdateUser(userId, true);
            if (!res)
                return $"User with ID {userId} cannot be activated";
            return $"User with ID {userId} activated";
        }

    }
}
