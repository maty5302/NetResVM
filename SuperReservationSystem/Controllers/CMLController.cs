using ApiCisco;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class CMLController : Controller
    {
        //make api connectionn library
        //Testing phase
        //rework to get url to server from file / from database 
        public required UserHttpClient client;
        private ServerService serverService = new ServerService();

        public IActionResult Index(int id,string servername)
        {
            if (servername == null)
            {
                TempData["ErrorMessage"] = "Server not specified";
                return RedirectToAction("Index", "Home");
            }
            if(User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }

            return RedirectToAction("LabList", "CML", new { servername = servername, id= id });
        }

        public async Task<IActionResult> LabList(int id, string servername)
        {
            if(servername==null)
            {
                TempData["ErrorMessage"] = "Server not specified";
                return RedirectToAction("Index", "Home");
            }
            var server = serverService.GetServerById(id);         
            if(server == null)
            {
                TempData["ErrorMessage"] = "Server not found";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ServerName = servername;
            ViewBag.ServerID = id;
            client = new UserHttpClient(server.IpAddress);
            var res = await Authentication.Authenticate(client, server.Username, server.Password);
            if (res != null && res.IsSuccessStatusCode)
            {
                var labs = Lab.GetLabs(client, res.Content.ReadAsStringAsync().Result);
                if (labs.Result != null)
                {
                    ViewBag.Labs2 = labs.Result; // CAREFUL! This is not a good practice, it's just for testing purposes
                    return View();
                }

                TempData["ErrorMessage"] = $"An error occurred. ";
                return RedirectToAction("Index", "Home");
            }
            else if(res != null)
            {
                TempData["ErrorMessage"] = $"An error occurred. {res.ReasonPhrase}";
                return RedirectToAction("Index", "Home");
            }
            TempData["ErrorMessage"] = "An error occurred. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> LabInfo(int id, string servername, string id_lab)
        {
            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found";
                return RedirectToAction("Index", "Home");
            }
            if(id_lab == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList","CML", new {id=id, servername=servername } );
            }
            ViewBag.ServerID = id;
            ViewBag.ServerName = servername;
            client = new UserHttpClient(server.IpAddress);
            var res = await Authentication.Authenticate(client, server.Username, server.Password); 
            if (res != null && res.IsSuccessStatusCode)
            {
                var lab = Lab.GetLabInfo(client, id_lab);
                if (lab.Result != null)
                {
                    ViewBag.Lab = lab.Result;
                    return View();
                }
                TempData["ErrorMessage"] = $"Lab not found.";
                return RedirectToAction("LabList","CML", new {id=id,servername=servername } );
            }
            return View();
        }
    }
}
