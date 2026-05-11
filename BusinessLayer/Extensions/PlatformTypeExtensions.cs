using BusinessLayer.Enum;
using BusinessLayer.Models;

namespace BusinessLayer.Extensions
{
    /// <summary>
    /// Provides extension methods for the PlatformType enumeration to retrieve platform-specific settings.
    /// </summary>
    /// <remarks>This static class contains methods that extend PlatformType with additional functionality,
    /// such as obtaining default protocol and file extension information for each supported platform. New platforms can
    /// be supported by adding corresponding cases in the extension methods.</remarks>
    public static class PlatformTypeExtensions
    {
        /// <summary>
        /// Retrieves the default platform settings for the specified platform type.
        /// </summary>
        /// <remarks>If the specified platform type is not recognized, default settings with protocol
        /// "http://" and file extension ".txt" are returned. Add support for additional platforms as needed.</remarks>
        /// <param name="platform">The platform type for which to obtain the default settings.</param>
        /// <returns>A <see cref="PlatformSettings"/> instance containing the default protocol and file extension for the
        /// specified platform.</returns>
        public static PlatformSettings GetSettings(this PlatformType platform)
        {
            return platform switch
            {
                PlatformType.CML => new PlatformSettings
                {
                    DefaultProtocol = "https://",
                    FileExtension = ".yaml"
                },
                PlatformType.EVE => new PlatformSettings
                {
                    DefaultProtocol = "http://",
                    FileExtension = ".zip" // Or ".unl" depending on what your adapter expects
                },
                // Add new platforms here in the future (e.g., GNS3)

                // Default fallback
                _ => new PlatformSettings
                {
                    DefaultProtocol = "http://",
                    FileExtension = ".txt"
                }
            };
        }
    }
}
