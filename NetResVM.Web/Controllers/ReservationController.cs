using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using SimpleLogger;
using System.Text;
using Microsoft.Extensions.Localization;
using NetResVM.Web.Models;

namespace NetResVM.Web.Controllers
{
    /// <summary>
    /// Controller for managing reservations.
    /// </summary>
    public class ReservationController : Controller
    {
        private readonly ServerService _serverService;
        private readonly UserService _userService;
        private readonly ReservationService _reservationService;
        private readonly IPlatformManager _platformManager;
        private readonly SimpleLogger.ILogger _logger = FileLogger.Instance;
        private readonly IStringLocalizer<SharedResource> _localizer;
        public ReservationController(IPlatformManager platformManager, ServerService serverService, UserService userService, ReservationService reservationService, IStringLocalizer<SharedResource> localizer)
        {
            _platformManager = platformManager;
            _serverService = serverService;
            _userService = userService;
            _reservationService = reservationService;
            _localizer = localizer;
        }

        /// <summary>
        /// Displays the list of reservations.
        /// </summary>
        /// <param name="month">Optional month for calendar view (1-12)</param>
        /// <param name="year">Optional year for calendar view</param>
        /// <param name="view">Optional active view ('list' or 'month')</param>
        /// <returns> An <see cref="IActionResult"/> that renders main reservations page </returns>
        public IActionResult Index(int? month, int? year, string? view = "list")
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            int targetMonth = (month.HasValue && month.Value >= 1 && month.Value <= 12) ? month.Value : DateTime.Now.Month;
            int targetYear = (year.HasValue && year.Value >= 2000 && year.Value <= 2100) ? year.Value : DateTime.Now.Year;

            var reservations = _reservationService.GetAllReservations();          
            List<ReservationInformationModel> plannedReservations = new List<ReservationInformationModel>();
            List<ReservationInformationModel> allReservations = new List<ReservationInformationModel>();
            if (reservations == null)
            {
                ViewBag.Reservations = plannedReservations;
                ViewBag.PlannedReservations = plannedReservations;
                ViewBag.AllReservations = allReservations;
                ViewBag.CurrentMonth = targetMonth;
                ViewBag.CurrentYear = targetYear;
                ViewBag.ActiveView = view ?? "list";
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
                // strictly this selected month and year
                if (reservation.ReservationStart.Month == targetMonth && reservation.ReservationStart.Year == targetYear)
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
            ViewBag.CurrentMonth = targetMonth;
            ViewBag.CurrentYear = targetYear;
            ViewBag.ActiveView = view ?? "list";
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
                    TempData["ErrorMessage"] = _localizer["UnsupportedPlatform"].Value;
                    _logger.LogError($"Unknown platform type: {platform} for server ID: {selectedServer.Value}");
                    return View("Create", model);
                }
                IVirtualizationAdapter adapter = _platformManager.GetAdapter(platform);
                var res = await adapter.GetLabsAsync(selectedServer.Value);
                if (res.Labs != null && res.Labs.Count > 0)
                {
                    ViewBag.Labs3 = new List<LabDTO>(res.Labs);
                }
                else
                {
                    ViewBag.Labs3 = new List<LabDTO>();
                    TempData["ErrorMessage"] = _localizer["NoLabsFound"].Value;
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
            var userId = _userService.GetUserId(User.Identity.Name);
            var reservations = _reservationService.GetReservationsByUserId(userId);
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
        [HttpPost]
        public IActionResult DeleteReservation(int reservationId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            
            var userId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);
            bool isAdmin = User.IsInRole("Admin");
            var reservation = _reservationService.DeleteReservation(reservationId, userId, isAdmin);
            if (reservation)
                TempData["SuccessMessage"] = _localizer["ReservationDeleted"].Value;
            else
                TempData["ErrorMessage"] = _localizer["SomethingWrong"].Value;
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
            var userId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);
            if (!selectedServer.HasValue || selectedServer.Value == 0)
            {
                TempData["ErrorMessage"] = _localizer["ServerNotSelected"].Value;
                _logger.LogWarning("Server not selected, while creating reservation");
                return RedirectToAction("Create");
            }
            else
                reserve.ServerId = selectedServer.Value;
            if (reserve.LabId == null)
            {
                TempData["ErrorMessage"] = _localizer["LabNotSelected"].Value;
                _logger.LogWarning("Lab not selected, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if (reserve.ReservationStart >= reserve.ReservationEnd || reserve.ReservationStart < DateTime.Now)
            {
                TempData["ErrorMessage"] = _localizer["InvalidTime"].Value;
                _logger.LogWarning("Invalid reservation time, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            if(reserve.ReservationStart.AddHours(1) > reserve.ReservationEnd)
            {
                TempData["ErrorMessage"] = _localizer["ResOneHour"].Value;
                _logger.LogWarning("Reservation must be at least 1 hour, while creating reservation");
                return RedirectToAction("Create", new { reserve, selectedServer });
            }
            //reservation only that day
            if(reserve.ReservationStart.Date != reserve.ReservationEnd.Date)
            {
                TempData["ErrorMessage"] = _localizer["ResSameDay"].Value;
                _logger.LogWarning("Reservation must be on the same day, while creating reservation");
                return RedirectToAction("Create", new {reserve,selectedServer});
            }
            if (userId <= 0)
            {
                TempData["ErrorMessage"] = _localizer["UserNotSelected"].Value;
                _logger.LogWarning("User not selected, while creating reservation");
                return RedirectToAction("Create");
            }
            var reservation = new ReservationModel
            {
                ServerId = selectedServer.Value,                
                LabId = reserve.LabId,
                ReservationStart = reserve.ReservationStart,
                ReservationEnd = reserve.ReservationEnd,
                UserId = userId
            };
            var result = _reservationService.MakeReservation(reservation);
            if (result)
                TempData["SuccessMessage"] = _localizer["ReservationCreated"].Value;
            else
                TempData["ErrorMessage"] = _localizer["ReservationExistsError"].Value;
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
            if (reservation == null)
            {
                TempData["ErrorMessage"] = _localizer["ReservationNotFound"].Value;
                _logger.LogWarning($"Reservation with ID {reservationId} not found for saving event.");
                return RedirectToAction("UserReservation", "Reservation");
            }

            return File(Encoding.UTF8.GetBytes(reservation.ToString()), "text/calendar", "reservation.ics");
        }

    }
}
