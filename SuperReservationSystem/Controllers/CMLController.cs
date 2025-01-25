using ApiCisco;
using ApiCisco.Model;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SuperReservationSystem.Controllers
{
    public class CMLController : Controller
    {
        private ServerService serverService = new ServerService();
        private ApiCiscoAuthService authService = new ApiCiscoAuthService();
        private ApiCiscoLabService labService = new ApiCiscoLabService();

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
            if (server==null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ServerName = server.Name;
            ViewBag.ServerID = id;
            
            var labs = await labService.GetLabs(id);
            if(labs.labs!=null)
            {
                ViewBag.Labs = labs.labs;
                return View();
            }

            TempData["ErrorMessage"] = $"An error occurred. {labs.Message}. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> LabInfo(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.ServerID = id;

            var lab = await labService.GetLabInfo(id, labId);
            if (lab.lab == null)
            {
                TempData["ErrorMessage"] = lab.Message;
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.Lab = lab.lab;
            return View();
        }

        public async Task<IActionResult> DownloadLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.ServerID = id;

            var data = await labService.DownloadLab(id, labId);
            if (data.fileContent == null)
            {
                TempData["ErrorMessage"] = data.message;
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            return File(data.fileContent, "text/plain", "lab.yaml");
        }

        public async Task<IActionResult> StartLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var result = await labService.StartLab(id, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab started successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = result.message;
            return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
        }

        public async Task<IActionResult> StopLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var result =await labService.StopLab(id, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab stopped successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = result.message;
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
            var res = await labService.ImportLab(serverId,file);
            if (res)
            {
                TempData["SuccessMessage"] = "Lab imported successfully.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabList", "CML", new { id = serverId });

        }

        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {            
            var client = await authService.AuthenticateAndCreateClient(serverId);
            if (client.conn == null)
            {
                TempData["ErrorMessage"] = client.message;
                return RedirectToAction("Index", "Home");
            }
            var nodes = Node.GetNodes(client.conn, labId);
            if (nodes.Result != null)
            {
                var nodesInfo = new List<NodeModel>();
                foreach (var item in nodes.Result)
                {
                    var node = Node.GetNodeInfo(client.conn, labId, item);
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
