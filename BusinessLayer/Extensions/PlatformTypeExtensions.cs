using BusinessLayer.Enum;
using BusinessLayer.Models;

namespace BusinessLayer.Extensions
{
    public static class PlatformTypeExtensions
    {
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
