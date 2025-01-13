using ApiCisco;
using ApiCisco.Model;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SuperReservationSystem.Controllers
{
    public class CMLController : Controller
    {
        private ServerService serverService = new ServerService();

        public async Task<UserHttpClient?> SetClientAndAuth(ServerModel? server)
        {
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return null;
            }

            UserHttpClient client = new UserHttpClient(server.IpAddress);
            var res = await Authentication.Authenticate(client, server.Username, server.Password);
            if (res != null && res.IsSuccessStatusCode)
            {
                return client;
            }
            else
            {
                TempData["ErrorMessage"] = "Authentication failed.";
                return null;
            }
        }

        public IActionResult Index(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            return RedirectToAction("LabList", "CML", new { id = id });
        }

        public async Task<IActionResult> LabList(int id)
        {
            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ServerName = server.Name;
            ViewBag.ServerID = id;

            var labsID = Lab.GetLabs(client);

            List<LabModel> labs = new List<LabModel>();
            if (labsID.Result != null)
            {
                foreach (var lab in labsID.Result)
                {
                    var labInfo = Lab.GetLabInfo(client, lab);
                    if(labInfo.Result!=null)
                        labs.Add(labInfo.Result);
                }
                ViewBag.Labs = labs;

                ViewBag.Labs2 = labsID.Result; // CAREFUL! This is not a good practice, it's just for testing purposes
                return View();
            }

            TempData["ErrorMessage"] = "An error occurred. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> LabInfo(int id, string labId)
        {
            //also search if lab is already owned
            var server = serverService.GetServerById(id);

            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }

            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ServerID = id;

            var lab = Lab.GetLabInfo(client, labId);
            if (lab.Result == null)
            {
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.Lab = lab.Result;
            return View();
        }

        public async Task<IActionResult> DownloadLab(int id, string labId)
        {
            var server = serverService.GetServerById(id);

            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }

            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ServerID = id;

            var lab = Lab.DownloadLab(client, labId);
            if (lab.Result == null)
            {
                TempData["ErrorMessage"] = $"Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            return File(Encoding.UTF8.GetBytes(lab.Result), "text/plain", "lab.yaml");
        }

        public async Task<IActionResult> StartLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var server = serverService.GetServerById(id);

            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var lab = Lab.StartLab(client, labId);
            if (lab.Result.Item1)
            {
                TempData["SuccessMessage"] = "Lab started successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = lab.Result.Item2;
            return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
        }

        public async Task<IActionResult> StopLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found";
                return RedirectToAction("Index", "Home");
            }
            var client = await SetClientAndAuth(server);

            if (client == null)
                return RedirectToAction("Index", "Home");

            var lab = Lab.StopLab(client, labId);
            if (lab.Result)
            {
                TempData["SuccessMessage"] = "Lab stopped successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = "Lab not found.";
            return RedirectToAction("LabList", "CML", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> ImportLab(IFormFile file, int serverId)
        {
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            var server = serverService.GetServerById(serverId);
            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            string fileContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                fileContent = await reader.ReadToEndAsync();
            }
            var lab = Lab.ImportLab(client, fileContent);
            if (lab.Result)
            {
                TempData["SuccessMessage"] = "Lab imported successfully.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabList", "CML", new { id = serverId });

        }

        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {
            var server = serverService.GetServerById(serverId);
            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var nodes = Node.GetNodes(client, labId);
            if (nodes.Result != null)
            {
                var nodesInfo = new List<NodeModel>();
                foreach (var item in nodes.Result)
                {
                    var node = Node.GetNodeInfo(client, labId, item);
                    nodesInfo.Add(node.Result);
                    ViewBag.Nodes = nodesInfo;
                }
                return View();
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabInfo", "CML", new { id = serverId, labId = labId });

        }
    }
}
