using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using SimpleLogger;
using System.Threading.Tasks;
using ILogger = SimpleLogger.ILogger;

namespace SuperReservationSystem
{
    public class BackgroundTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ReservationService _reservationService = new ReservationService();
        private readonly ServerService _serverService = new ServerService();
        private readonly ApiEVELabService _apiEVELabService = new ApiEVELabService();
        private readonly ApiEVENodeService _apiEVENodeService = new ApiEVENodeService();
        private readonly ApiCiscoLabService _apiCiscoLabService = new ApiCiscoLabService();
        private readonly ILogger _logger = FileLogger.Instance;

        /// <summary>
        /// Starts Reservation of a lab by stopping all of them before.
        /// </summary>
        /// <param name="serverType"> Type of server (CML or EVE) </param>
        /// <param name="serverID"> ID of server </param>
        /// <param name="labId"> ID of a lab within server specified by ID </param>
        private async void StartReservation(string serverType, int serverID, string labId)
        {
            if(serverType=="CML")
            {
                var labState = await _apiCiscoLabService.GetState(serverID, labId);
                if (labState != null && labState == "STARTED")
                {
                    _logger.Log($"Lab {labId} already started");
                }
                else if (labState != null && labState == "STOPPED")
                {

                    var stopped = await _apiCiscoLabService.StopAllLabs(serverID);
                    if (stopped.value)
                    {
                        var res = await _apiCiscoLabService.StartLab(serverID, labId);
                        if (res.Item1)
                            _logger.Log($"Lab {labId} on server {serverID} started");
                        else
                            _logger.LogWarning($"Lab {labId} on server {serverID} could not be started");
                    }
                    else
                        _logger.LogWarning($"All labs on server {serverID} could not be stopped");
                }
                else
                    _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
            }
            else if(serverType=="EVE")
            {
                var eve = await _apiEVELabService.GetLabInfoById(serverID, labId);
                if (eve != null)
                {
                    var status = await _apiEVELabService.StateOfLab(serverID, eve.Name);
                    if (status == 0)
                    {
                        var res = await _apiEVENodeService.StartAllNodes(serverID, eve.Filename);
                        if (res)
                            _logger.Log($"Lab {labId} on server {serverID} started");
                        else
                            _logger.LogWarning($"Lab {labId} on server {serverID} could not be started");
                    }
                    else if (status == 2)
                    {
                        _logger.Log($"Lab {labId} already started");
                    }
                    else
                        _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
                }
                else
                    _logger.LogError($"Could not find a lab {labId} on {serverID} - {serverType}");
            }
            else
                _logger.LogError($"Unknown server type {serverType}. Skipping..");
        }

        /// <summary>
        /// Stops reservation that is running on a server.
        /// </summary>
        /// <param name="serverType"> Type of server (CML or EVE) </param>
        /// <param name="serverID"> ID of server </param>
        /// <param name="labId"> ID of a lab within server specified by ID </param>
        private async void StopReservation(string serverType,int serverId, string labId)
        {
            if (serverType == "CML")
            {
                var labState = await _apiCiscoLabService.GetState(serverId, labId);
                if (labState != null && labState == "STARTED")
                {
                    var res = await _apiCiscoLabService.StopLab(serverId, labId);
                    if (res.value)
                        _logger.Log($"Lab {labId} on server {serverId} stopped");
                    else
                        _logger.LogWarning($"Lab {labId} on server {serverId} could not be stopped");
                }
                else if(labState != null && labState =="STOPPED")
                {
                    _logger.LogWarning($"Lab {labId} on server {serverId} already stopped");
                }
                else
                    _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");

            }
            else if(serverType =="EVE")
            {
                var eve = await _apiEVELabService.GetLabInfoById(serverId, labId);
                if (eve != null)
                {
                    var status = await _apiEVELabService.StateOfLab(serverId, eve.Name);
                    if (status == 2)
                    {
                        var res = await _apiEVENodeService.StopAllNodes(serverId, eve.Filename);
                        if (res)
                        {
                            _logger.Log($"Lab {labId} on server {serverId} stopped");
                        }
                        else
                            _logger.LogWarning($"Lab {labId} on server {serverId} could not be stopped");
                    }
                    else if(status==0)
                    {
                        _logger.Log($"Lab {labId} already stopped.");
                    }
                    else
                        _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
                }
                else
                    _logger.LogError($"Could not find a lab {labId} on {serverId} - {serverType}");
            }
            else
                _logger.LogError($"Unknown server type {serverType}. Skipping..");
        }

        /// <summary>
        /// Checks all reservations for each server. Can also delete expired reservations and calls method for starting and stopping reservations.
        /// </summary>
        private void CheckReservations()
        {
            try
            {
                var reservations = _reservationService.GetAllReservations();

                var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                _logger.Log($"Checking reservations..{time}");
                if (reservations != null)
                {
                    foreach (var reservation in reservations)
                    {
                        //getting type of server for start correct actions for each type of server
                        var serverType = _serverService.GetServerType(reservation.ServerId);
                        if (serverType == "" || serverType == null)
                        {
                            _logger.LogError("Server not found.");
                            continue;
                        }
                        //delete expire reservations after 6 months by default
                        if (reservation.ReservationEnd.AddMonths(6) < DateTime.Now)
                        {
                            _reservationService.DeleteReservation(reservation.Id);
                        }
                        //checking if reservation has to be started, if so then start it
                        if ((reservation.ReservationStart == time || (reservation.ReservationStart < time && reservation.ReservationEnd > time)))
                        {
                            StartReservation(serverType, reservation.ServerId, reservation.LabId);
                        }
                        //checking if reservation is already over, if so then stops it
                        if (reservation.ReservationEnd <= time && reservation.ReservationEnd.AddMinutes(5) > time)
                        {
                            StopReservation(serverType, reservation.ServerId, reservation.LabId);
                        }
                    }
                }
                else
                    _logger.LogWarning("No reservations were found...");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
        /// <summary>
        /// Starts a background task which checks reservations every minute.
        /// </summary>
        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {   // Check reservations every minute
                        CheckReservations();
                        await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {                        
                        _logger.LogWarning("Task was cancelled");
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exceptions
                        _logger.LogError(ex.Message);
                    }
                }
            },_cancellationTokenSource.Token);
        }
        /// <summary>
        /// Stops the background task by cancelling token
        /// </summary>
        public void Stop()
        {
            // Cancel the task
            _cancellationTokenSource.Cancel();
            _logger.LogWarning("Background task for checking reservations was cancelled.");
        }

    }
}
