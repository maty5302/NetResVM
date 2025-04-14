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
    /// <summary>
    /// This class is responsible for establishing a connection to the database.
    /// </summary>
    internal class DBConnector
    {
        static ILogger _logger = FileLogger.Instance;
        /// <summary>
        /// This method returns a SqlConnection object that can be used to connect to the database.
        /// </summary>
        /// <returns> Returns established SQL connection </returns>
        /// <exception cref="DatabaseConfigurationException"> Exception is triggered if sqlconnection.json has invalid settings or missing settings </exception>
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

        /// <summary>
        /// This method reads the SQL connection settings from a JSON file and returns a SqlConnectionStringBuilder object.
        /// </summary>
        /// <returns> SqlConnectionStringBuilder </returns>
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