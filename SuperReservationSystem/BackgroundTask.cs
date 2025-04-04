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
        /// Starts Reservation of a lab by stopping all of them before
        /// </summary>
        /// <param name="serverType"></param>
        /// <param name="serverID"></param>
        /// <param name="labId"></param>
        private async void StartReservation(string serverType, int serverID, string labId)
        {
            if(serverType=="CML")
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
            else if(serverType=="EVE")
            {
                var stopped = true; //hardcoded testing - implement method for stopping all labs EVE
                if(stopped)
                {
                    var eve = await _apiEVELabService.GetLabInfoById(serverID,labId);
                    if (eve != null)
                    {
                        var res = await _apiEVENodeService.StartAllNodes(serverID, eve.Filename);
                        if (res)
                        {
                            _logger.Log($"Lab {labId} on server {serverID} started");
                        }
                        else
                            _logger.LogWarning($"Lab {labId} on server {serverID} could not be started");
                    }
                    else
                        _logger.LogError($"Could not find a lab {labId} on {serverID} - {serverType}");
                }
                else
                    _logger.LogWarning($"All labs on server {serverID} could not be stopped");
            }
            else
                _logger.LogError($"Unknown server type {serverType}. Skipping..");
        }

        private async void CheckReservations()
        {
            try
            {
                var reservations = _reservationService.GetAllReservations();
                
                var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
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
                        else if (reservation.ReservationEnd <= time && serverType == "CML")
                        {
                            var labState = await _apiCiscoLabService.GetState(reservation.ServerId, reservation.LabId);
                            if (labState != null && labState == "started")
                            {
                                var res = await _apiCiscoLabService.StopLab(reservation.ServerId, reservation.LabId);
                                if (res.value)
                                    FileLogger.Instance.Log("Lab stopped");
                                else
                                    FileLogger.Instance.LogWarning("Lab could not be stopped");
                            }
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

        public void Stop()
        {
            // Cancel the task
            _cancellationTokenSource.Cancel();
            _logger.LogWarning("Background task for checking reservations was cancelled.");
        }

    }
}
