using BusinessLayer.Enum;

namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a virtualization server configuration used for lab management.
    /// </summary>
    public class ServerModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the server.
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// Gets or sets the platform type for the current context.
        /// </summary>
        public PlatformType Platform { get; set;  }

        /// <summary>
        /// Gets or sets the display name of the server.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the server.
        /// </summary>
        public required string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the username used to authenticate with the server.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// Gets or sets the password used to authenticate with the server.
        /// </summary>
        public required string Password { get; set; }
    }

}
