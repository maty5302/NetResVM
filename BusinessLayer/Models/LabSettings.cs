namespace BusinessLayer.Models
{
    /// <summary>
    /// Application-wide settings related to running labs on virtualization servers.
    /// </summary>
    /// <remarks>Values are loaded once at application startup from configuration (section <c>LabSettings</c> in
    /// appsettings.json, or environment variable <c>LabSettings__MaxRunningLabsPerServer</c> in Docker).
    /// The settings are static so they are shared by the web application and the Telnet console,
    /// which create virtualization adapters independently.</remarks>
    public static class LabSettings
    {
        /// <summary>
        /// Default maximum number of labs that can run simultaneously on one server.
        /// </summary>
        public const int DefaultMaxRunningLabsPerServer = 2;

        private static int _maxRunningLabsPerServer = DefaultMaxRunningLabsPerServer;

        /// <summary>
        /// Gets or sets the maximum number of labs that can run simultaneously on one server.
        /// Values lower than 1 are ignored and the default value is used instead.
        /// </summary>
        public static int MaxRunningLabsPerServer
        {
            get => _maxRunningLabsPerServer;
            set => _maxRunningLabsPerServer = value >= 1 ? value : DefaultMaxRunningLabsPerServer;
        }
    }
}
