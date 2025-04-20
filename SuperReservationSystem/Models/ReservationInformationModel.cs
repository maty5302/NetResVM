using BusinessLayer.Models;

namespace SuperReservationSystem.Models
{
    /// <summary>
    /// Model for displaying reservation information.
    /// </summary>
    public class ReservationInformationModel : ReservationModel
    {
        /// <summary>
        /// Gets or sets the name of the server where the lab is hosted.
        /// </summary>
        public required string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the type of the server.
        /// </summary>
        public required string ServerType { get; set; }

        /// <summary>
        /// Gets or sets the name of the lab that is reserved.
        /// </summary>
        public required string UserName { get; set; }
    }
}
