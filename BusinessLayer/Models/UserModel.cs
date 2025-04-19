namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// Gets or sets the password of the user. Can be null if the user authenticates via VSB.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the role of the user. Can be "admin" or "student".
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Gets or sets the type of authorization. Can be "localhost" or "vsb".
        /// </summary>
        public required string AuthorizationType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is active.
        /// </summary>
        public bool Active { get; set; }
    }

}
