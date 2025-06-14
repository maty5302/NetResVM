﻿using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;
using SimpleLogger;
using SuperReservationSystem.Models;
using System.Text;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing reservations.
    /// </summary>
    public class ReservationController : Controller
    {
        ServerService serverService = new ServerService();
        UserService userService = new UserService();
        ReservationService reservationService = new ReservationService();
        ApiCiscoLabService labServiceCisco = new ApiCiscoLabService();
        ApiEVELabService labServiceEve = new ApiEVELabService();
        SimpleLogger.ILogger logger = FileLogger.Instance;

        /// <summary>
        /// Displays the list of reservations.
        /// </summary>
        /// <returns> An <see cref="IActionResult"/> that renders main reservations page </returns>
        public IActionResult Index()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            
            var reservations = reservationService.GetAllReservations();          
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
                //this month
                if(reservation.ReservationStart.Month==DateTime.Now.Month)
                allReservations.Add(new ReservationInformationModel
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
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var servers = serverService.GetAllServers();
            ViewBag.Servers = servers;

            // if server is selected, get the labs for that server
            if (selectedServer.HasValue)
            {
                model.ServerId = selectedServer.Value;
                var serverType = serverService.GetServerType(selectedServer.Value);               
                if (serverType == "CML")
                {
                    // get the labs for that server
                    var res = await labServiceCisco.GetLabs(selectedServer.Value);

                    CiscoLabModel? labSelected = null;
                    if (labId != null)
                    {
                        // get the lab info
                        labSelected = (await labServiceCisco.GetLabInfo(selectedServer.Value, labId)).lab;
                        if (labId != null && labSelected != null)
                            model.LabId = labId;
                    }

                    //add labs to the list and view them on page if there are any
                    if (res.labs != null && res.labs.Count > 0)
                    {
                        ViewBag.Labs3 = new List<ILabModel>(res.labs);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Cannot connect to server. Try again..";
                    }
                }
                else if (serverType == "EVE")
                {
                    // get the labs for that server
                    var res = await labServiceEve.GetLabs(selectedServer.Value);
                    //add labs to the list and view them on page if there are any
                    if (res != null && res.Count > 0)
                    {
                        ViewBag.Labs3 = new List<ILabModel>(res);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Cannot connect to server. Try again..";
                    }
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
            var UserId = userService.GetUserId(User.Identity.Name);
            var reservations = reservationService.GetReservationsByUserId(UserId);
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

        /// <summary>
        /// Deletes a reservation by its ID.
        /// </summary>
        /// <param name="reservationId"> Id of the reservation </param>
        /// <returns>  An <see cref="IActionResult"/> that redirects to user reservation and informs if reservation was deleted successfully </returns>
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
            var reservation = reservationService.SaveReservation(reservationId);

            return File(Encoding.UTF8.GetBytes(reservation.ToString()), "text/calendar", "reservation.ics");
        }

    }
}
