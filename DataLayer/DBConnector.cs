using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SimpleLogger;

namespace DataLayer
{
    internal class DBConnector
    {
        static ILogger _logger = FileLogger.Instance;
        public static SqlConnection GetConnection()
        {
            var builder = GetBuilder();
            if (builder == null || string.IsNullOrEmpty(builder.UserID) || string.IsNullOrEmpty(builder.Password) || string.IsNullOrEmpty(builder.DataSource))
            {
                _logger.LogError("Database connection settings are missing or invalid.");
            
                throw new DatabaseConfigurationException("Database connection settings are missing or invalid.");                
            }
            
            return new SqlConnection(builder.ConnectionString);
        }

        private static SqlConnectionStringBuilder? GetBuilder()
        {
            var file = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText("sqlconnection.json"));
            if (file != null && file.TryGetValue("DataSource", out var dataSourceValue) && file.TryGetValue("UserID", out var userIdValue) && file.TryGetValue("Password", out var passValue))
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = dataSourceValue;
                builder.UserID = userIdValue;
                builder.Password = passValue;
                builder.InitialCatalog = "DB_NetResVM";
                return builder;
            }
            else
                return null;
        }
    }
}