using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a reservation for using a specific lab on a server by a user.
    /// </summary>
    public class ReservationModel : IValidatableObject
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
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public required DateTime ReservationStart { get; set; }

        /// <summary>
        /// Gets or sets the end time of the reservation.
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public required DateTime ReservationEnd { get; set; }

        /// <summary>
        /// Validates the reservation model, ensuring that the start time is before the end time.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReservationStart >= ReservationEnd)
            {
                yield return new ValidationResult(
                    "Reservation start time must be before end time.",
                    [nameof(ReservationStart), nameof(ReservationEnd)]);
            }
        }
    }

}
