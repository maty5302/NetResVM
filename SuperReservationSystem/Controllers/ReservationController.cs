using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using SimpleLogger;
using NetResVM.Models;
using System.Text;

namespace NetResVM.Controllers
{
    /// <summary>
    /// Controller for managing reservations.
    /// </summary>
    public class ReservationController : Controller
    {
        private readonly ServerService _serverService;
        private readonly UserService _userService;
        private readonly ReservationService _reservationService;
        private readonly PlatformManager _platformManager;
        private readonly SimpleLogger.ILogger _logger = FileLogger.Instance;
        public ReservationController(PlatformManager platformManager, ServerService serverService, UserService userService, ReservationService reservationService)
        {
            _platformManager = platformManager;
            _serverService = serverService;
            _userService = userService;
            _reservationService = reservationService;
        }

        /// <summary>
        /// Displays the list of reservations.
        /// </summary>
        /// <returns> An <see cref="IActionResult"/> that renders main reservations page </returns>
        public IActionResult Index()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            
            var reservations = _reservationService.GetAllReservations();          
            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            List<ReservationInformationModel> allReservations = new List<ReservationInformationModel>();
            if (reservations == null)
            {
                ViewBag.Reservations = plannedReservations;
                return View();
            }
            // goes though all reservations and adds them to the list
            foreach (var reservation in reservations)
            {
                var server = _serverService.GetServerById(reservation.ServerId);
                if (server == null)
                    continue;
                var user = _userService.GetUsername(reservation.UserId);
                if (reservation.ReservationEnd > DateTime.Now)
                    plannedReservations.Add(new ReservationInformationModel
                    {
                        Id = reservation.Id,
                        ServerName = server.Name,
                        Platform = server.Platform,
                        ServerId = server.Id,
                        LabId = reservation.LabId,
                        ReservationStart = reservation.ReservationStart,
                        ReservationEnd = reservation.ReservationEnd,
                        UserId = reservation.UserId,
                        UserName = user
                    });
                //this month
                if(reservation.ReservationStart.Month==DateTime.Now.Month)
                allReservations.Add(new ReservationInformationModel
                {
                    Id = reservation.Id,
                    ServerName = server.Name,
                    Platform = server.Platform,
                    ServerId = server.Id,
                    LabId = reservation.LabId,
                    ReservationStart = reservation.ReservationStart,
                    ReservationEnd = reservation.ReservationEnd,
                    UserId = reservation.UserId,
                    UserName = user
                });
            }
            ViewBag.AllReservations = allReservations;
            ViewBag.PlannedReservations = plannedReservations;
            return View();
        }

        /// <summary>
        /// Displays the reservation creation page.
        /// </summary>
        /// <param name="model"> Model of reservation where information about it are stored </param>
        /// <param name="selectedServer"> indicate if server is selected and ID if it do </param>
        /// <param name="labId"> ID of a lab </param>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders main create reservation page </returns>
        public async Task<IActionResult> Create(ReservationModel model, int? selectedServer, string? labId)
        {
            model.ReservationStart = DateTime.Now.AddMinutes(2);
            model.ReservationEnd = DateTime.Now.AddMinutes(62);
            
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var servers = _serverService.GetAllServers();
            ViewBag.Servers = servers;

            // if server is selected, get the labs for that server
            if (selectedServer.HasValue)
            {
                model.ServerId = selectedServer.Value;
                var platform = _serverService.GetServerType(selectedServer.Value);
                if(platform == PlatformType.Unknown)
                {
                    TempData["ErrorMessage"] = "Unknown platform type.";
                    _logger.LogError($"Unknown platform type: {platform} for server ID: {selectedServer.Value}");
                    return View("Create", model);
                }
                IVirtualizationAdapter adapter = _platformManager.GetAdapter(platform);
                var res = await adapter.GetLabsAsync(selectedServer.Value);
                LabDTO? selected = null;
                if(labId != null)
                {
                    var labInfo = await adapter.GetLabInfoAsync(selectedServer.Value,labId);
                    if (labInfo.Lab != null)
                    {
                        selected = labInfo.Lab;
                        model.LabId = labId;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Cannot get lab info. Try again..";
                        _logger.LogError($"Cannot get lab info for lab ID: {labId} on server ID: {selectedServer.Value}. Message: {labInfo.Message}");
                        return View("Create", model);
                    }
                }
                if (res.Labs != null && res.Labs.Count > 0)
                {
                    ViewBag.Labs3 = new List<LabDTO>(res.Labs);
                }
                else
                {
                    ViewBag.Labs3 = new List<LabDTO>();
                    TempData["ErrorMessage"] = "No labs found for the selected server.";
                    _logger.LogWarning($"No labs found for server ID: {selectedServer.Value}. Message: {res.Message}");
                }
            }

            return View("Create", model);
        }

        /// <summary>
        /// Displays the reservation page for a specific user.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders page for user reservations </returns>
        public IActionResult UserReservation()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var UserId = _userService.GetUserId(User.Identity.Name);
            var reservations = _reservationService.GetReservationsByUserId(UserId);
            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            List<ReservationInformationModel> expiredReservations = new List<ReservationInformationModel>();
            if (reservations == null)
            {
                ViewBag.Reservations = plannedReservations;
                ViewBag.ExpiredReservations = expiredReservations;
                return View();
            }
            foreach (var reservation in reservations)
            {
                var server = _serverService.GetServerById(reservation.ServerId);
                if (server == null)
                    continue;
                var user = _userService.GetUsername(reservation.UserId);
                if (reservation.ReservationEnd > DateTime.Now)
                    plannedReservations.Add(new ReservationInformationModel
                    {
                        Id = reservation.Id,
                        ServerName = server.Name,
                        Platform = server.Platform,
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
                        Platform = server.Platform,
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

        /// <summary>
        /// Deletes a reservation by its ID.
        /// </summary>
        /// <param name="reservationId"> Id of the reservation </param>
        /// <returns>  An <see cref="IActionResult"/> that redirects to user reservation and informs if reservation was deleted successfully </returns>
        public IActionResult DeleteReservation(int reservationId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var reservation = _reservationService.DeleteReservation(reservationId);
            if (reservation)
                TempData["SuccessMessage"] = "Reservation deleted.";
            else
                TempData["ErrorMessage"] = "Something went wrong. See log.";
            return RedirectToAction("UserReservation", "Reservation");
        }

        /// <summary>
        /// Creates a reservation based on the provided model and selected server.
        /// </summary>
        /// <param name="reserve"> Model contains all information about reservation </param>
        /// <param name="selectedServer"> indicate if server is selected and ID if it do </param>
        /// <returns> An <see cref="IActionResult"/> that redirects to create or home page of reservations depends on if reservation was successful </returns>
        [HttpPost]
        public IActionResult MakeReservation(ReservationModel reserve, int? selectedServer)
        {
            if (User.Identity!=null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var UserId = _userService.GetUserId(User.Identity.Name);
            if (!selectedServer.HasValue && selectedServer == 0 || selectedServer==null)
            {
                TempData["ErrorMessage"] = "Server not selected.";
                _logger.LogWarning("Server not selected, while creating reservation");
                return RedirectToAction("Create");
            }
            else
                reserve.ServerId = selectedServer.Value;
            if (reserve.LabId == null)
            {
                TempData["ErrorMessage"] = "Lab not selected.";
                _logger.LogWarning("Lab not selected, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if (reserve.ReservationStart >= reserve.ReservationEnd || reserve.ReservationStart < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid reservation time.";
                _logger.LogWarning("Invalid reservation time, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if(reserve.ReservationStart.AddHours(1) > reserve.ReservationEnd)
            {
                TempData["ErrorMessage"] = "Reservation must be at least 1 hour.";
                _logger.LogWarning("Reservation must be at least 1 hour, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            //reservation only that day
            if(reserve.ReservationStart.Date != reserve.ReservationEnd.Date)
            {
                TempData["ErrorMessage"] = "Reservation start and end must be on the same day.";
                _logger.LogWarning("Reservation must be on the same day, while creating reservation");
                return RedirectToAction("Create", new {reserve,selectedServer});
            }
            if (reserve.UserId == -1)
            {
                TempData["ErrorMessage"] = "User not selected.";
                _logger.LogWarning("User not selected, while creating reservation");
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
            var result = _reservationService.MakeReservation(reservation);
            if (result)
                TempData["SuccessMessage"] = "Reservation created.";
            else
                TempData["ErrorMessage"] = "Reservation exists at this time or error occurred.";
            return RedirectToAction("Index","Reservation");
        }

        /// <summary>
        /// Saves the reservation as an .ics file for the specified reservation ID.
        /// </summary>
        /// <param name="reservationId"> Id of the reservation </param>
        /// <returns>  An <see cref="IActionResult"/> that shows dialog to save ics file </returns>
        public IActionResult SaveEvent(int reservationId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var reservation = _reservationService.SaveReservation(reservationId);

            return File(Encoding.UTF8.GetBytes(reservation.ToString()), "text/calendar", "reservation.ics");
        }

    }
}
