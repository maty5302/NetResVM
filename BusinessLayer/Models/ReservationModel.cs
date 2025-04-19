namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a reservation for using a specific lab on a server by a user.
    /// </summary>
    public class ReservationModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the reservation.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who made the reservation.
        /// </summary>
        public required int UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the server where the lab is hosted.
        /// </summary>
        public required int ServerId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the lab that is reserved.
        /// </summary>
        public required string LabId { get; set; }

        /// <summary>
        /// Gets or sets the start time of the reservation.
        /// </summary>
        public required DateTime ReservationStart { get; set; }

        /// <summary>
        /// Gets or sets the end time of the reservation.
        /// </summary>
        public required DateTime ReservationEnd { get; set; }
    }

}
