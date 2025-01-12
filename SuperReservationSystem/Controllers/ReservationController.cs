using ApiCisco;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using SimpleLogger;
using SuperReservationSystem.Models;

namespace SuperReservationSystem.Controllers
{
    public class ReservationController : Controller
    {
        ServerService serverService = new ServerService();
        UserService userService = new UserService();
        ReservationService reservationService = new ReservationService();
        SimpleLogger.ILogger logger = FileLogger.Instance;

        public IActionResult Index()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
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


        public async Task<IActionResult> Create(ReservationModel model, int? selectedServer, string? labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
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
                if (res != null && res.IsSuccessStatusCode)
                {
                    var labs = await Lab.GetLabs(userHttpClient);

                    if (labs != null && labs.Length > 0)
                    {
                        ViewBag.Labs3 = labs;
                        if (labId != null && labs.Contains(labId))
                            model.LabId = labId;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot connect to server. Try again..";
                }
            }

            return View("Create", model);
        }

        public IActionResult UserReservation()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var UserId = userService.GetUserId(User.Identity.Name);
            var reservations = reservationService.GetReservationsByUserId(UserId);
            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            List<ReservationInformationModel> expiredReservations = new List<ReservationInformationModel>();
            foreach (var reservation in reservations)
            {
                var server = serverService.GetServerById(reservation.ServerId);
                var user = userService.GetUsername(reservation.UserId);
                if (reservation.ReservationEnd > DateTime.Now)
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
                else
                    expiredReservations.Add(new ReservationInformationModel
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
            ViewBag.PlannedReservations = plannedReservations;
            ViewBag.ExpiredReservations = expiredReservations;

            return View();
        }

        public IActionResult DeleteReservation(int reservationId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var reservation = reservationService.DeleteReservation(reservationId);
            if (reservation)
                TempData["SuccessMessage"] = "Reservation deleted.";
            else
                TempData["ErrorMessage"] = "Something went wrong. See log.";
            return RedirectToAction("UserReservation", "Reservation");
        }


        [HttpPost]
        public IActionResult MakeReservation(ReservationModel reserve, int? selectedServer)
        {
            if (User.Identity!=null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var UserId = userService.GetUserId(User.Identity.Name);
            if (!selectedServer.HasValue && selectedServer == 0)
            {
                TempData["ErrorMessage"] = "Server not selected.";
                logger.LogWarning("Server not selected, while creating reservation");
                return RedirectToAction("Create");
            }
            else
                reserve.ServerId = selectedServer.Value;
            if (reserve.LabId == null)
            {
                TempData["ErrorMessage"] = "Lab not selected.";
                logger.LogWarning("Lab not selected, while creating reservation");
                return RedirectToAction("Create", reserve);
            }
            if (reserve.ReservationStart >= reserve.ReservationEnd || reserve.ReservationStart < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid reservation time.";
                logger.LogWarning("Invalid reservation time, while creating reservation");
                return RedirectToAction("Create",reserve);
            }
            if (reserve.UserId == -1)
            {
                TempData["ErrorMessage"] = "User not selected.";
                logger.LogWarning("User not selected, while creating reservation");
                return RedirectToAction("Create",reserve);
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
