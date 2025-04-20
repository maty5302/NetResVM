namespace SuperReservationSystem.Models
{
    /// <summary>
    /// Model for changing the password.
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// Old password of the user.
        /// </summary>
        public required string oldPassword { get; set; }

        /// <summary>
        /// New password of the user.
        /// </summary>
        public required string newPassword { get; set; }

        /// <summary>
        /// Confirmation of the new password.
        /// </summary>
        public required string confirmPassword { get; set; }
    }
}
