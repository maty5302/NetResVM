using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using SimpleLogger;
using ILogger = SimpleLogger.ILogger;

namespace NetResVM
{
    /// <summary>
    /// Class for background task that checks reservations and starts/stops them.
    /// </summary>
    public class BackgroundTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger _logger = FileLogger.Instance;
        private readonly IServiceProvider _serviceProvider;
        public BackgroundTask(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        /// <summary>
        /// Attempts to start a reservation for the specified lab on the given platform and server.
        /// </summary>
        /// <remarks>If the lab is already started or running, no action is taken. If the lab is stopped,
        /// all labs on the server are stopped before attempting to start the specified lab. Errors are logged if the
        /// lab state is unknown or if labs cannot be retrieved.</remarks>
        /// <param name="platformManager">The platform manager used to obtain the appropriate virtualization adapter for the operation. Cannot be
        /// null.</param>
        /// <param name="serverType">The type of platform server on which the lab resides.</param>
        /// <param name="serverId">The unique identifier of the server hosting the lab.</param>
        /// <param name="labId">The identifier of the lab to start the reservation for. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task StartReservation(IPlatformManager platformManager, PlatformType serverType, int serverId, string labId)
        {
            IVirtualizationAdapter adapter = platformManager.GetAdapter(serverType);
            var labState = await adapter.StateOfLab(serverId, labId);
            if (labState == null)
            {
                _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
            }
            else if(labState == "STARTED" || labState == "RUNNING")
            {
                _logger.Log($"Lab {labId} already started");
            }
            else if (labState == "STOPPED")
            {
                var labs = await adapter.GetLabsAsync(serverId);
                if(labs.Labs == null)
                {
                    _logger.LogError($"Labs on server {serverId} could not be retrieved. Cannot perform the operation");
                    return;
                }
                foreach (var lab in labs.Labs)
                {
                    _logger.Log($"Stopping lab {lab.Id} on server {serverId} before starting reservation");
                    await adapter.StopLabAsync(serverId, lab.Id);                    
                }
                var res = await adapter.StartLabAsync(serverId, labId);
                if (res.value)
                    _logger.Log($"Lab {labId} on server {serverId} started");
                else
                    _logger.LogWarning($"Lab {labId} on server {serverId} could not be started");

            }
             else
                _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
        }

        /// <summary>
        /// Attempts to stop a lab reservation on the specified platform and server.
        /// </summary>
        /// <remarks>If the lab is already stopped, a log entry is created indicating no action was taken.
        /// If the lab is in an unknown state, an error is logged and no stop operation is attempted.</remarks>
        /// <param name="platformManager">The platform manager used to obtain the appropriate virtualization adapter.</param>
        /// <param name="serverType">The type of platform server on which the lab is running.</param>
        /// <param name="serverId">The unique identifier of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab to stop.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        private async Task StopReservation(IPlatformManager platformManager,PlatformType serverType,int serverId, string labId)
        {
            IVirtualizationAdapter adapter = platformManager.GetAdapter(serverType);
            var labState = await adapter.StateOfLab(serverId, labId);
            if (labState == "STARTED" || labState == "RUNNING")
            {
                var res = await adapter.StopLabAsync(serverId, labId);
                if (res.value)
                    _logger.Log($"Lab {labId} on server {serverId} stopped");
                else
                    _logger.LogWarning($"Lab {labId} on server {serverId} could not be stopped");

            }
            else if (labState == "STOPPED")
            {
                _logger.Log($"Lab {labId} already stopped.");
            }
            else {
                _logger.LogError($"Lab {labId} in unknown state. Cannot perform the operation");
            }
        }

        /// <summary>
        /// Checks all current reservations and performs necessary actions such as starting or stopping reservations
        /// based on their scheduled times and removing expired reservations.
        /// </summary>
        /// <remarks>This method retrieves all reservations and processes each one according to its
        /// schedule. Reservations that have expired for more than six months are deleted. Reservations are started or
        /// stopped based on the current time relative to their scheduled start and end times. Logging is performed for
        /// key actions and errors. This method is intended to be called periodically to maintain reservation
        /// state.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CheckReservations()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<ReservationService>();
                    var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
                    var platformManager = scope.ServiceProvider.GetRequiredService<IPlatformManager>();

                    var reservations = reservationService.GetAllReservations();
                    var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);

                    _logger.Log($"Checking reservations..{time}");

                    if (reservations != null)
                    {
                        foreach (var reservation in reservations)
                        {
                            var serverType = serverService.GetServerType(reservation.ServerId);
                            if (serverType == PlatformType.Unknown)
                            {
                                _logger.LogError("Server not found or Unknown platform.");
                                continue;
                            }
                            if (reservation.ReservationEnd.AddMonths(6) < DateTime.Now)
                            {
                                reservationService.DeleteReservation(reservation.Id);
                            }
                            if ((reservation.ReservationStart == time || (reservation.ReservationStart < time && reservation.ReservationEnd > time)))
                            {
                                await StartReservation(platformManager, serverType, reservation.ServerId, reservation.LabId);
                            }
                            if (reservation.ReservationEnd <= time && reservation.ReservationEnd.AddMinutes(5) > time)
                            {
                                await StopReservation(platformManager, serverType, reservation.ServerId, reservation.LabId);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No reservations were found...");
                    }
                } 
            }
            catch (Exception e)
            {
                _logger.LogError($"CheckReservations failed: {e.Message}");
            }
        }

        /// <summary>
        /// Starts the background process that periodically checks reservations until cancellation is requested.
        /// </summary>
        /// <remarks>This method initiates an asynchronous loop that runs in the background. The process
        /// continues to execute until the associated cancellation token is triggered. If called multiple times without
        /// stopping the previous process, multiple background tasks may be started. Ensure proper cancellation to avoid
        /// resource leaks.</remarks>
        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        DateTime nextMinute = now.AddMinutes(1).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);

                        TimeSpan delay = nextMinute - now;

                        await Task.Delay(delay, _cancellationTokenSource.Token);
                        await CheckReservations();
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogWarning("Task was cancelled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }, _cancellationTokenSource.Token);
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
