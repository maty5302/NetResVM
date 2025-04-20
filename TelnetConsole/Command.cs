using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;

namespace TelnetConsole
{
    /// <summary>
    /// Class that handles commands for managing servers, labs, and users.
    /// </summary>
    internal static class Command
    {
        private static ServerService server = new ServerService();
        /// <summary>
        /// Lists all servers in the system.
        /// </summary>
        /// <returns> A response string based on the result of the ListServers </returns>
        public static string ListServers()
        {
            var list = server.GetAllServers();
            if (list == null)
                return "";

            int idWidth = 5, ipWidth = 15, typeWidth = 12;
            var sb = new StringBuilder();
            sb.AppendLine($"{"ID".PadRight(idWidth)} | {"IP Address".PadRight(ipWidth)} | {"Server Type".PadRight(typeWidth)}");
            foreach (var item in list)
            {
                sb.AppendLine($"{item.Id.ToString().PadRight(idWidth)} | {item.IpAddress.PadRight(ipWidth)} | {item.ServerType.PadRight(typeWidth)}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Lists all labs on a specified server.
        /// </summary>
        /// <param name="server_id"> ID of server we want labs from </param>
        /// <returns>  A response string based on the result of the ListLabs</returns>
        public static string ListLabs(string server_id)
        {
            int id = -1;
            int.TryParse(server_id, out id);
            if (id < 0)
                return "Invalid ID";
            var sb = new StringBuilder();
            int idWidth = 40, nameWidth = 25, dateWidth = 12;
            sb.AppendLine($"{"ID".PadRight(idWidth)} | {"Name".PadRight(nameWidth)} | {"Last Modified".PadRight(dateWidth)}");
            var type = server.GetServerType(int.Parse(server_id));
            if (type == "CML")
            {
                return ListCMLLabs(server_id, sb, idWidth, nameWidth, dateWidth);
            }
            else if (type == "EVE")
                return ListEVELabs(server_id, sb, idWidth, nameWidth, dateWidth);
            else
                return "";
        }

        /// <summary>
        /// Retrieves and formats a list of labs from a specific Cisco Modeling Labs (CML) server.
        /// </summary>
        /// <param name="server_id">The ID of the server from which to retrieve labs.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> used to accumulate the formatted lab information.</param>
        /// <param name="idWidth">The width to pad the lab ID column.</param>
        /// <param name="nameWidth">The width to pad the lab name column.</param>
        /// <param name="dateWidth">The width to pad the last modified date column.</param>
        /// <returns>
        /// A formatted string representing the list of labs, or an error message if the labs could not be retrieved.
        /// </returns>
        private static string ListCMLLabs(string server_id, StringBuilder sb, int idWidth, int nameWidth, int dateWidth)
        {
            ApiCiscoLabService service = new ApiCiscoLabService();
            var labs = service.GetLabs(int.Parse(server_id)).Result;
            if (labs.labs == null)
                return labs.Message;
            foreach (var lab in labs.labs)
                sb.AppendLine($"{lab.Id.ToString().PadRight(idWidth)} | {lab.Name.PadRight(nameWidth)} | {lab.Last_modified.ToShortDateString().PadRight(dateWidth)}");
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves and formats a list of labs from a specific EVE-NG server.
        /// </summary>
        /// <param name="server_id">The ID of the server from which to retrieve labs.</param>
        /// <param name="sb">A <see cref="StringBuilder"/> used to accumulate the formatted lab information.</param>
        /// <param name="idWidth">The width to pad the lab ID column.</param>
        /// <param name="nameWidth">The width to pad the lab name column.</param>
        /// <param name="dateWidth">The width to pad the last modified date column.</param>
        /// <returns>
        /// A formatted string representing the list of labs, or an error message if the labs could not be retrieved.
        /// </returns>
        private static string ListEVELabs(string server_id, StringBuilder sb, int idWidth, int nameWidth, int dateWidth)
        {
            ApiEVELabService service = new ApiEVELabService();
            var labs = service.GetLabs(int.Parse(server_id)).Result;
            if (labs == null)
                return "";
            foreach (var lab in labs)
                sb.AppendLine($"{lab.Id.ToString().PadRight(idWidth)} | {lab.Name.PadRight(nameWidth)} | {lab.Last_modified.ToShortDateString().PadRight(dateWidth)}");
            return sb.ToString();

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
            if (users == null)
                return "";
            foreach (var user in users)
            {
                sb.AppendLine($"{user.Id.ToString().PadRight(idWidth)} | {user.Username.ToString().PadRight(nameWidth)} | {user.Active.ToString().PadRight(activeWidth)}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Starts a lab on a specified server based on the server type.
        /// </summary>
        /// <param name="server_id"> ID of server where a lab to run is</param>
        /// <param name="lab_id"> Lab ID which we want to run </param>
        /// <returns> A string indicating success, failure, or an unknown server type. </returns>
        public static string StartLab(string server_id, string lab_id)
        {
            int id = -1;
            int.TryParse(server_id, out id);
            if (id < 0)
                return "Invalid ID";
            var type = server.GetServerType(id);
            if (type == "CML")
                return StartCMLLab(server_id, lab_id);
            else if (type == "EVE")
                return StartEVELab(server_id, lab_id);
            else
                return "Unknown serverID";
        }

        /// <summary>
        /// Stops a lab on a specified server based on the server type.
        /// </summary>
        /// <param name="server_id"> ID of server where a lab to stop is</param>
        /// <param name="lab_id"> Lab ID which we want to stop </param>
        /// <returns> A string indicating success, failure, or an unknown server type. </returns>
        public static string StopLab(string server_id, string lab_id)
        {
            int id = -1;
            int.TryParse(server_id, out id);
            if (id < 0)
                return "Invalid ID";
            var type = server.GetServerType(id);
            if (type == "CML")
                return StopCMLLab(server_id, lab_id);
            else if (type == "EVE")
                return StopEVELab(server_id, lab_id);
            else
                return "Unknown serverID";
        }

        /// <summary>
        /// Stops an EVE-NG lab by stopping all associated nodes.
        /// </summary>
        /// <param name="server_id">ID of the EVE-NG server.</param>
        /// <param name="lab_id">ID of the lab to stop.</param>
        /// <returns>
        /// A string indicating whether the lab was stopped successfully or an error message.
        /// </returns>
        private static string StopEVELab(string server_id, string lab_id)
        {
            ApiEVELabService labService = new ApiEVELabService();
            ApiEVENodeService nodeService = new ApiEVENodeService();
            var lab = labService.GetLabInfoById(int.Parse(server_id), lab_id).Result;
            if (lab == null)
                return "Cannot find a lab";
            var res = nodeService.StartAllNodes(int.Parse(server_id), lab.Filename).Result;
            if (res)
                return "Lab stopped successfully";
            return "Lab cannot be stopped";
        }

        /// <summary>
        /// Stops a Cisco Modeling Labs (CML) lab on the given server.
        /// </summary>
        /// <param name="server_id">ID of the CML server.</param>
        /// <param name="lab_id">ID of the lab to stop.</param>
        /// <returns>
        /// A string indicating whether the lab was stopped successfully or an error message from the API.
        /// </returns>
        private static string StopCMLLab(string server_id, string lab_id)
        {
            ApiCiscoLabService service = new ApiCiscoLabService();
            var res = service.StopLab(int.Parse(server_id), lab_id).Result;
            if (res.value == false)
                return res.message;
            else
                return "Lab stopped successfully";
        }

        /// <summary>
        /// Starts a Cisco Modeling Labs (CML) lab on the given server.
        /// </summary>
        /// <param name="server_id">ID of the CML server.</param>
        /// <param name="lab_id">ID of the lab to start.</param>
        /// <returns>
        /// A string indicating whether the lab was started successfully or an error message from the API.
        /// </returns>
        private static string StartCMLLab(string server_id, string lab_id) {
            ApiCiscoLabService service = new ApiCiscoLabService();
            var res = service.StartLab(int.Parse(server_id), lab_id).Result;
            if (res.value == false)
                return res.message;
            else
                return "Lab started successfully";
        }

        /// <summary>
        /// Starts an EVE-NG lab by starting all associated nodes.
        /// </summary>
        /// <param name="server_id">ID of the EVE-NG server.</param>
        /// <param name="lab_id">ID of the lab to start.</param>
        /// <returns>
        /// A string indicating whether the lab was started successfully or an error message.
        /// </returns>
        private static string StartEVELab(string server_id, string lab_id)
        {
            ApiEVELabService labService = new ApiEVELabService();
            ApiEVENodeService nodeService = new ApiEVENodeService();
            var lab = labService.GetLabInfoById(int.Parse(server_id), lab_id).Result;
            if (lab == null)
                return "Cannot find a lab";
            var res = nodeService.StartAllNodes(int.Parse(server_id), lab.Filename).Result;
            if (res)
                return "Lab started successfully";
            return "Lab cannot be started";
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
