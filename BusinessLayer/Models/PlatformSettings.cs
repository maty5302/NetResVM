namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents configuration settings related to platform-specific protocols and file extensions.
    /// </summary>
    public class PlatformSettings
    {
        /// <summary>
        /// Gets or sets the default protocol used for network communication.
        /// </summary>
        public string DefaultProtocol { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file extension associated with the file, including the leading period.
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;
    }
}
