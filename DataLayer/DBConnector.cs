using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataLayer
{
    internal class DBConnector
    {
        public static SqlConnection GetConnection()
        {
            var builder = GetBuilder();
            if (builder == null || string.IsNullOrEmpty(builder.UserID) || string.IsNullOrEmpty(builder.Password) || string.IsNullOrEmpty(builder.DataSource))
            {
                throw new DatabaseConfigurationException("Database connection settings are missing or invalid.");
            }

            return new SqlConnection(builder.ConnectionString);
        }

        public static SqlConnectionStringBuilder? GetBuilder()
        {
            var file = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText("sqlconnection.json"));
            if (file.TryGetValue("DataSource", out var dataSourceValue) && file.TryGetValue("UserID", out var userIdValue) && file.TryGetValue("Password", out var passValue))
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
