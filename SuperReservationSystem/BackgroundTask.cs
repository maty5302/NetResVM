using ApiCisco;
using BusinessLayer.Services;
using System.Threading.Tasks;

namespace SuperReservationSystem
{
    public class BackgroundTask
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ReservationService _reservationService = new ReservationService();
        private ServerService _serverService = new ServerService();

        public async void CheckReservations()
        {
            var reservations = _reservationService.GetAllReservations();
            var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            Console.WriteLine($"Checking reservations..{time}");
            foreach (var reservation in reservations)
            {
                var server = _serverService.GetServerById(reservation.ServerId);
                UserHttpClient client = new UserHttpClient(server.IpAddress);
                //if (reservation.ReservationEnd < DateTime.Now)
                //{
                //    _reservationService.DeleteReservation(reservation.Id);
                //}
                if (reservation.ReservationStart == time && server !=null && server.ServerType=="CML")
                {
                    await Authentication.Authenticate(client, server.Username, server.Password);
                    var res = await Lab.StartLab(client, reservation.LabId);
                    Console.WriteLine(res.ToString());
                }
                else if(reservation.ReservationEnd==time && server != null && server.ServerType == "CML")
                {
                    await Authentication.Authenticate(client, server.Username, server.Password);
                    var res = await Lab.StopLab(client, reservation.LabId);
                    Console.WriteLine(res.ToString());

                }
            }
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        CheckReservations();
                        await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception ex)
                    {
                        // Log or handle exceptions
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            },_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

    }
}
