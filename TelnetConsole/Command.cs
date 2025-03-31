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
    internal static class Command
    {
        private static ServerService server = new ServerService();
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

        private static string StopCMLLab(string server_id, string lab_id)
        {
            ApiCiscoLabService service = new ApiCiscoLabService();
            var res = service.StopLab(int.Parse(server_id), lab_id).Result;
            if (res.value == false)
                return res.message;
            else
                return "Lab stopped successfully";
        }

        private static string StartCMLLab(string server_id, string lab_id) {
            ApiCiscoLabService service = new ApiCiscoLabService();
            var res = service.StartLab(int.Parse(server_id), lab_id).Result;
            if (res.value == false)
                return res.message;
            else
                return "Lab started successfully";
        }

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

        public static string DeactivateUser(int userId)
        {
            UserService userService = new UserService();
            var res = userService.UpdateUser(userId, false);
            if (!res)
                return $"User with ID {userId} cannot be deactivated";
            return $"User with ID {userId} deactivated";
        }

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
