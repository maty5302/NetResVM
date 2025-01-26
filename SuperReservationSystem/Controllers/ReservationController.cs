using ApiCisco;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using Microsoft.AspNetCore.Mvc;
using SimpleLogger;
using SuperReservationSystem.Models;
using System.Linq;

namespace SuperReservationSystem.Controllers
{
    public class ReservationController : Controller
    {
        ServerService serverService = new ServerService();
        UserService userService = new UserService();
        ReservationService reservationService = new ReservationService();
        ApiCiscoAuthService authService = new ApiCiscoAuthService();
        ApiCiscoLabService labService = new ApiCiscoLabService();
        SimpleLogger.ILogger logger = FileLogger.Instance;

        public IActionResult Index()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            var reservations = reservationService.GetAllReservations();          
            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            if (reservations == null)
            {
                ViewBag.Reservations = plannedReservations;
                return View();
            }

            foreach (var reservation in reservations)
            {
                var server = serverService.GetServerById(reservation.ServerId);
                if (server == null)
                    continue;
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
            

            if (selectedServer.HasValue)
            {
                model.ServerId = selectedServer.Value;
                var res = await labService.GetLabs(selectedServer.Value);

                LabModel? labSelected = null;                
                if(labId!=null)
                {
                    var lab = await labService.GetLabInfo(selectedServer.Value,labId);
                    labSelected = lab.lab;
                }                

                if (res.labs != null && res.labs.Count > 0)
                {
                    ViewBag.Labs3 = res.labs;
                    if (labId != null && labSelected!=null)
                        model.LabId = labId;
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
                if (server == null)
                    continue;
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
            if (!selectedServer.HasValue && selectedServer == 0 || selectedServer==null)
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
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if (reserve.ReservationStart >= reserve.ReservationEnd || reserve.ReservationStart < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid reservation time.";
                logger.LogWarning("Invalid reservation time, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if(reserve.ReservationStart.AddHours(1) > reserve.ReservationEnd)
            {
                TempData["ErrorMessage"] = "Reservation must be at least 1 hour.";
                logger.LogWarning("Reservation must be at least 1 hour, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            //reservation only that day
            if(reserve.ReservationStart.Date != reserve.ReservationEnd.Date)
            {
                TempData["ErrorMessage"] = "Reservation start and end must be on the same day.";
                logger.LogWarning("Reservation must be on the same day, while creating reservation");
                return RedirectToAction("Create", new {reserve,selectedServer});
            }
            if (reserve.UserId == -1)
            {
                TempData["ErrorMessage"] = "User not selected.";
                logger.LogWarning("User not selected, while creating reservation");
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
