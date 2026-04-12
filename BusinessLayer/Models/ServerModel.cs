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
        /// Gets or sets the type of the server (e.g., "cisco", "eve-ng").
        /// </summary>
        public required string ServerType { get; set; }

        /// <summary>
        /// Gets or sets the PlatformType strongly-typed enum based on ServerType.
        /// </summary>
        public PlatformType Platform
        {
            get
            {
                // Druhý parametr 'true' ignoruje velikost písmen (např. "Cisco" vs "cisco")
                return System.Enum.TryParse<PlatformType>(ServerType, true, out var platformType)
                    ? platformType
                    : default; // Vrací výchozí hodnotu enumu, pokud parsování selže
            }
            set
            {
                // Při nastavení enumu se automaticky aktualizuje stringová reprezentace
                ServerType = value.ToString().ToLower(); // .ToLower() podle vaší konvence
            }
        }

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
