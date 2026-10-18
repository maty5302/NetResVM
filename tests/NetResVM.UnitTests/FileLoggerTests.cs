using SimpleLogger;
using Xunit;

namespace NetResVM.UnitTests
{
    public class FileLoggerTests
    {
        [Fact]
        public void FileLogger_Instance_ShouldNotBeNull()
        {
            var logger = FileLogger.Instance;
            Assert.NotNull(logger);
        }

        [Fact]
        public void FileLogger_LogMethods_ShouldNotThrowException()
        {
            var logger = FileLogger.Instance;

            var exInfo = Record.Exception(() => logger.Log("Unit test info message"));
            var exWarning = Record.Exception(() => logger.LogWarning("Unit test warning message"));
            var exError = Record.Exception(() => logger.LogError("Unit test error message"));

            Assert.Null(exInfo);
            Assert.Null(exWarning);
            Assert.Null(exError);
        }

        [Fact]
        public async Task FileLogger_ConcurrentLogging_ShouldBeThreadSafe()
        {
            var logger = FileLogger.Instance;
            var tasks = new List<Task>();

            for (int i = 0; i < 50; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    logger.Log($"Concurrent test log message {index}");
                    logger.LogWarning($"Concurrent test warning {index}");
                    logger.LogError($"Concurrent test error {index}");
                }));
            }

            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }
    }
}
