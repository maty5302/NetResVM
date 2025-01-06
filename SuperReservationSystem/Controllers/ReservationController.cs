using ApiCisco;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;

namespace SuperReservationSystem.Controllers
{
    public class ReservationController : Controller
    {
        ServerService serverService = new ServerService();
        UserService userService = new UserService();
        ReservationService reservationService = new ReservationService();

        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            var reservations = reservationService.GetAllReservations();

            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            foreach (var reservation in reservations)
            {
                var server = serverService.GetServerById(reservation.ServerId);
                var user = userService.GetUsername(reservation.UserId);
                if(reservation.ReservationEnd>DateTime.Now)
                plannedReservations.Add(new ReservationInformationModel
                {
                    Id = reservation.Id,
                    ServerName = server.Name,
                    ServerType = server.ServerType,
                    ServerId = server.Id,
                    LabId = reservation.LabId,
                    ReservationStart = reservation.ReservationStart,
                    ReservationEnd = reservation.ReservationEnd,
                    UserId = reservation.UserId,
                    UserName = user
                });
            }

            ViewBag.Reservations = plannedReservations;

            return View();
        }


        public async Task<IActionResult> Create(ReservationModel model, int? selectedServer)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var servers = serverService.GetAllServers();
            ViewBag.Servers = servers;
            ViewBag.Labs3 = new string[0]; // Initialize with empty array to avoid null reference

            if (selectedServer.HasValue)
            {
                model.ServerId = selectedServer.Value;
                var selectedServerModel = serverService.GetServerById(selectedServer.Value);
                var userHttpClient = new UserHttpClient(selectedServerModel.IpAddress);
                var res = await Authentication.Authenticate(userHttpClient, selectedServerModel.Username, selectedServerModel.Password);
                if (res!=null && res.IsSuccessStatusCode) {
                    var labs = await Lab.GetLabs(userHttpClient);

                    if (labs != null && labs.Length > 0)
                    {
                        ViewBag.Labs3 = labs;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot connect to server. Try again..";
                }
            }

            return View("Create", model);
        }


        [HttpPost]
        public IActionResult MakeReservation(ReservationModel reserve, int? selectedServer)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var UserId = userService.GetUserId(User.Identity.Name);
            if (!selectedServer.HasValue && selectedServer == 0)
            {
                TempData["ErrorMessage"] = "Server not selected.";
                return RedirectToAction("Create");
            }
            if (reserve.LabId == null)
            {
                TempData["ErrorMessage"] = "Lab not selected.";
                return RedirectToAction("Create");
            }
            if (reserve.ReservationStart >= reserve.ReservationEnd)
            {
                TempData["ErrorMessage"] = "Invalid reservation time.";
                return RedirectToAction("Create");
            }
            if (reserve.ReservationStart < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid reservation time.";
                return RedirectToAction("Create");
            }
            if (reserve.UserId == -1)
            {
                TempData["ErrorMessage"] = "User not selected.";
                return RedirectToAction("Create");
            }
            var reservation = new ReservationModel
            {
                ServerId = selectedServer.Value,
                LabId = reserve.LabId,
                ReservationStart = reserve.ReservationStart,
                ReservationEnd = reserve.ReservationEnd,
                UserId = UserId
            };
            var result = reservationService.MakeReservation(reservation);
            if (result)
                TempData["SuccessMessage"] = "Reservation created.";
            else
                TempData["ErrorMessage"] = "Something went wrong. See log.";
            return RedirectToAction("Index","Reservation");
        }

    }
}
