using BusinessLayer.Enum;
using BusinessLayer.Models;

namespace NetResVM.Models
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
        /// Gets or sets the name of the lab that is reserved.
        /// </summary>
        public required string UserName { get; set; }
    }
}
