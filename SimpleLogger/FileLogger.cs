namespace SimpleLogger
{
    public class FileLogger : ILogger
    {
        private static readonly Lazy<FileLogger> _instance = new Lazy<FileLogger>(() => new FileLogger());
        public static FileLogger Instance
        {
            get { return _instance.Value; }
        }
        private string CurrentDirectory;
        private string _logFilePath;

        private FileLogger()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
            string logDirectory = Path.Combine(CurrentDirectory, "logs");
            Directory.CreateDirectory(logDirectory); // Ensure the directory exists
            _logFilePath = Path.Combine(logDirectory, $"log-{DateTime.Today:yyyy-MM-dd}.txt");
        }

        private void WriteLog(string messageType, string message)
        {
            try
            {
                using (var writer = new StreamWriter(_logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {messageType}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        public void Log(string message) => WriteLog("INFO", message);

        public void LogError(string message) => WriteLog("ERROR", message);

        public void LogWarning(string message) => WriteLog("WARNING", message);
    }
}
