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
        //Testing phase
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
            if(User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }            
            return RedirectToAction("LabList", "CML", new { id=id });
        }

        public async Task<IActionResult> LabList(int id)
        {
            var server = serverService.GetServerById(id);         
            
            var client = await SetClientAndAuth(server);
            if(client==null)
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
                    labs.Add(labInfo.Result);
                }
                ViewBag.Labs = labs;

                ViewBag.Labs2 = labsID.Result; // CAREFUL! This is not a good practice, it's just for testing purposes
                return View();
            }

            TempData["ErrorMessage"] = "An error occurred. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> LabInfo(int id, string servername, string id_lab)
        {
            //also search if lab is already owned
            var server = serverService.GetServerById(id);

            if(id_lab == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList","CML", new {id=id, servername=servername } );
            }

            var client = await SetClientAndAuth(server);
            if(client==null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ServerID = id;
            ViewBag.ServerName = server.Name;

            var lab = Lab.GetLabInfo(client, id_lab);
            if (lab.Result == null)
            {
                return RedirectToAction("LabList", "CML", new { id = id, servername = server.Name });
            }
            ViewBag.Lab = lab.Result;
            return View();
        }

        public async Task<IActionResult> DownloadLab(int id, string servername, string id_lab)
        {
            var server = serverService.GetServerById(id);
            
            if (id_lab == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id, servername = server.Name });
            }

            var client = await SetClientAndAuth(server);
            if(client==null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ServerID = id;
            ViewBag.ServerName = server.Name;
                        
            var lab = Lab.DownloadLab(client, id_lab);
            if (lab.Result == null)
            {
                TempData["ErrorMessage"] = $"Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id, servername = servername });
            }
            return File(Encoding.UTF8.GetBytes(lab.Result), "text/plain", "lab.yaml");
        }

        public async Task<IActionResult> StartLab(int id, string lab_id)
        {
            if (lab_id == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var server = serverService.GetServerById(id);

            var client = await SetClientAndAuth(server);
            if(client==null)
            {
                return RedirectToAction("Index", "Home");
            }
            var lab = Lab.StartLab(client, lab_id);
            if (lab.Result)
            {
                TempData["SuccessMessage"] = "Lab started successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, servername = server.Name,id_lab=lab_id });
            }
            TempData["ErrorMessage"] = "Lab not found.";
            return RedirectToAction("LabList", "CML", new { id = id, servername = server.Name });
        }

        public async Task<IActionResult> StopLab(int id, string lab_id)
        {
            if (lab_id == null)
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
           
            var lab = Lab.StopLab(client, lab_id);
            if (lab.Result)
            {
                TempData["SuccessMessage"] = "Lab stopped successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, servername = server.Name, id_lab = lab_id });
            }
            TempData["ErrorMessage"] = "Lab not found.";
            return RedirectToAction("LabList", "CML", new { id = id, servername = server.Name });            
        }

        [HttpPost]
        public async Task<IActionResult> ImportLab(IFormFile file, int serverId)
        {
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("LabList", "CML" , new { id = serverId });
            }
            var server = serverService.GetServerById(serverId);
            var client = await SetClientAndAuth(server);
            if (client == null)
            {
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            string fileContent;
            using(var reader=new StreamReader(file.OpenReadStream()))
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
    }
}
