using BusinessLayer.Models;

namespace SuperReservationSystem.Models
{
    public class ReservationInformationModel : ReservationModel
    {
        public required string ServerName { get; set; }
        public required string ServerType { get; set; }
        public required string UserName { get; set; }
    }
}
