using System.ComponentModel.DataAnnotations;

namespace SuperReservationSystem.Models
{
    /// <summary>
    /// Model for the login page.
    /// </summary>
    public class LoginModel
    {
        /// <summary>
        /// Username of the user.
        /// </summary>
        [Required]
        public required string Username { get; set; }

        /// <summary>
        /// Password of the user.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
