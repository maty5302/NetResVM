using SimpleLogger;

namespace DataLayer
{
    /// <summary>
    /// Idempotent schema migrations applied on application startup.
    /// </summary>
    public static class DatabaseMigrations
    {
        private static readonly ILogger _logger = FileLogger.Instance;

        /// <summary>
        /// Applies all pending migrations. Safe to call on every startup.
        /// </summary>
        public static void Apply()
        {
            EnsureUserLabOwnershipOrderColumn();
        }

        /// <summary>
        /// Adds the OwnershipID identity column to UserLabOwnership if it is missing.
        /// The column keeps the order in which owners were assigned (first = main owner);
        /// without it the queries ordered by OwnershipID would fail.
        /// </summary>
        private static void EnsureUserLabOwnershipOrderColumn()
        {
            string query = @"
                IF OBJECT_ID(N'dbo.UserLabOwnership', N'U') IS NOT NULL
                   AND COL_LENGTH('dbo.UserLabOwnership', 'OwnershipID') IS NULL
                BEGIN
                    ALTER TABLE dbo.UserLabOwnership ADD OwnershipID INT IDENTITY(1,1) NOT NULL;
                    SELECT 1;
                END
                ELSE
                    SELECT 0;";
            const int maxRetries = 10;
            const int delayMs = 3000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var connection = DBConnector.GetConnection())
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = query;
                            if (Convert.ToInt32(command.ExecuteScalar()) == 1)
                            {
                                _logger.LogWarning("Migration: added column UserLabOwnership.OwnershipID. Order of existing owners is not known - check the main owner of labs with more owners.");
                            }
                        }
                    }
                    return;
                }
                catch (DatabaseConfigurationException e)
                {
                    _logger.LogError($"Migration of UserLabOwnership.OwnershipID aborted: {e.Message}");
                    return;
                }
                catch (Exception e)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError($"Migration of UserLabOwnership.OwnershipID failed after {maxRetries} attempts: {e.Message}");
                    }
                    else
                    {
                        _logger.LogWarning($"Migration of UserLabOwnership.OwnershipID attempt {attempt}/{maxRetries} failed: {e.Message}. Retrying in {delayMs / 1000}s...");
                        Thread.Sleep(delayMs);
                    }
                }
            }
        }
    }
}
