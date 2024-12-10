using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class ServerTableDataGateway
    {
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
