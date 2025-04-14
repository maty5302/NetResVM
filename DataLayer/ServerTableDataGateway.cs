using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    /// <summary>
    /// This class is responsible for interacting with the Server table in the database.
    /// </summary>
    public class ServerTableDataGateway
    {
        /// <summary>
        /// Retrieves all servers from the Server table.
        /// </summary>
        /// <returns></returns>
        public DataTable GetAllServers()
        {
            string query = "SELECT * FROM Server";
            var result = new DataTable();

            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Retrieves a server by its ID from the Server table.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <returns></returns>
        public DataTable GetServerById(int id)
        {
            string query = "SELECT * FROM Server WHERE ServerID = @Id";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Inserts a new server into the Server table.
        /// </summary>
        /// <param name="serverType"> Type of the server (CML/EVE)</param>
        /// <param name="name"> Name of the server </param>
        /// <param name="ipAddress"> URI formated IP address</param>
        /// <param name="username"> Username for login to server </param>
        /// <param name="password"> Password for login to server </param>
        public void InsertServer(string serverType, string name, string ipAddress, string username, string password)
        {
            string query = "INSERT INTO Server (ServerType, Name, IpAddress, Username, Password) VALUES (@ServerType, @Name, @IpAddress, @Username, @Password)";
           
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ServerType", serverType);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@IpAddress", ipAddress);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Updates an existing server in the Server table.
        /// </summary>
        /// <param name="id"> ID of the server to be updated </param>
        /// <param name="serverType"> Type of the server (CML/EVE) </param>
        /// <param name="name"> Name of the server </param>
        /// <param name="ipAddress"> URI formated IP address </param>
        /// <param name="username"> Username for login to server </param>
        /// <param name="password"> Password for login to server </param>
        public void UpdateServer(int id, string serverType, string name, string ipAddress, string username, string password)
        {
            string query = "UPDATE Server SET ServerType = @ServerType, Name = @Name, IpAddress = @IpAddress, Username = @Username, Password = @Password WHERE ServerID = @Id";
            using(var connection=DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ServerType", serverType);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@IpAddress", ipAddress);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Removes an existing server from the Server table.
        /// </summary>
        /// <param name="id"> ID of the server to be removed </param>
        public void RemoveServer(int id)
        {
            string query = "DELETE FROM Server WHERE ServerID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
