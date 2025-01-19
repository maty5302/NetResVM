using ApiCisco;
using BusinessLayer.Services;
using SimpleLogger;
using System.Threading.Tasks;

namespace SuperReservationSystem
{
    public class BackgroundTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ReservationService _reservationService = new ReservationService();
        private readonly ServerService _serverService = new ServerService();

        private async void CheckReservations()
        {
            try
            {
                var reservations = _reservationService.GetAllReservations();
                var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                FileLogger.Instance.Log($"Checking reservations..{time}");
                if (reservations != null)
                {
                    foreach (var reservation in reservations)
                    {
                        var server = _serverService.GetServerById(reservation.ServerId);
                        if (server == null)
                        {
                            FileLogger.Instance.LogError("Server not found.");
                            continue;
                        }

                        UserHttpClient client = new UserHttpClient(server.IpAddress); 
                        if (reservation.ReservationEnd.AddMonths(6) < DateTime.Now)
                        {
                            _reservationService.DeleteReservation(reservation.Id);
                        }

                        if (reservation.ReservationStart == time && server.ServerType == "CML") //reservation.ReservationStart<time && reservation.ReservationEnd > time && //lab not running?
                        {
                            await Authentication.Authenticate(client, server.Username, server.Password);
                            var stopped = await Lab.StopAllLabs(client);
                            if (stopped)
                            {
                                var res = await Lab.StartLab(client, reservation.LabId);
                                if (res.Item1)
                                    FileLogger.Instance.Log("Lab started");
                                else
                                    FileLogger.Instance.LogWarning("Lab could not be started");
                            }
                            else
                                FileLogger.Instance.LogWarning("Labs could not be stopped");
                        }
                        else if (reservation.ReservationEnd <= time && server.ServerType == "CML")
                        {
                            await Authentication.Authenticate(client, server.Username, server.Password);
                            var res = await Lab.StopLab(client, reservation.LabId);
                            if (res)
                                FileLogger.Instance.Log("Lab stopped");
                            else
                                FileLogger.Instance.LogWarning("Lab could not be stopped");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FileLogger.Instance.LogError(e.Message);
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
                        FileLogger.Instance.LogWarning("Task was cancelled");
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exceptions
                        FileLogger.Instance.LogError(ex.Message);
                    }
                }
            },_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            // Cancel the task
            _cancellationTokenSource.Cancel();
            FileLogger.Instance.LogWarning("Background task for checking reservations was cancelled.");
        }

    }
}
