namespace SimpleLogger
{
    /// <summary>
    /// Singleton class that handles logging to a file.
    /// Thread-safe and supports daily file rotation.
    /// </summary>
    public class FileLogger : ILogger
    {
        private static readonly Lazy<FileLogger> _instance = new Lazy<FileLogger>(() => new FileLogger());
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance of the <see cref="FileLogger"/> class.
        /// </summary>
        public static FileLogger Instance
        {
            get { return _instance.Value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        private FileLogger()
        {
            EnsureLogsDirectoryExists();
        }

        private static void EnsureLogsDirectoryExists()
        {
            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch
            {
                // Suppress if directory creation fails during startup
            }
        }

        private static string GetLogFilePath()
        {
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            return Path.Combine(logDirectory, $"log-{DateTime.Today:yyyy-MM-dd}.txt");
        }

        /// <summary>
        /// Writes a log message to the log file with the specified message type.
        /// Thread-safe via lock synchronization.
        /// </summary>
        /// <param name="messageType">The type of message (INFO, ERROR, WARNING).</param>
        /// <param name="message">The log message to write.</param>
        private void WriteLog(string messageType, string message)
        {
            lock (_lock)
            {
                try
                {
                    string logFilePath = GetLogFilePath();
                    using (var writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {messageType}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    // If the log writing fails, write the exception message to the console.
                    Console.WriteLine($"Failed to write log: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Logs an informational message to the log file.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        public void Log(string message) => WriteLog("INFO", message);

        /// <summary>
        /// Logs an error message to the log file.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public void LogError(string message) => WriteLog("ERROR", message);

        /// <summary>
        /// Logs a warning message to the log file.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public void LogWarning(string message) => WriteLog("WARNING", message);
    }
}
