namespace BusinessLayer.Models
{
    /// <summary>
    /// Application-wide settings related to reservations.
    /// </summary>
    /// <remarks>Values are loaded once at application startup from configuration (section <c>ReservationSettings</c> in
    /// appsettings.json, or environment variable <c>ReservationSettings__MinReservationMinutes</c> in Docker).</remarks>
    public static class ReservationSettings
    {
        /// <summary>
        /// Default minimum length of a reservation in minutes.
        /// </summary>
        public const int DefaultMinReservationMinutes = 60;

        private static int _minReservationMinutes = DefaultMinReservationMinutes;

        /// <summary>
        /// Gets or sets the minimum length of a reservation in minutes.
        /// Values lower than 1 are ignored and the default value is used instead.
        /// </summary>
        public static int MinReservationMinutes
        {
            get => _minReservationMinutes;
            set => _minReservationMinutes = value >= 1 ? value : DefaultMinReservationMinutes;
        }
    }
}
