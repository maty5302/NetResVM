using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class UserTableDataGateway
    {
        public DataTable GetUserByUsername(string Username)
        {
            string query = "SELECT * FROM \"User\" WHERE Username=@username";
            var result = new DataTable();
           
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", Username);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        public DataTable GetUserById(int Id)
        {
            string query = "SELECT * FROM \"User\" WHERE UserID = @Id";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        public DataTable GetAllUsers()
        {
            string query = "SELECT * FROM \"User\"";
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

        //In future AuthorizationType will be added
        public void AddUser(string Username, string Password, string Role, int Active)
        {
            string query = "INSERT INTO \"User\" (Username, Password, Role, Active) VALUES (@username, @password, @role, @active)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", Username);
                    command.Parameters.AddWithValue("@password", Password);
                    command.Parameters.AddWithValue("@role", Role);
                    command.Parameters.AddWithValue("@active", Active);
                    command.ExecuteNonQuery();
                }
            }
        }

        //Update method for Active atribute
        public void UpdateUserActive(int Id, int Active)
        {
            string query = "UPDATE \"User\" SET Active = @active WHERE UserID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.Parameters.AddWithValue("@active", Active);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserPassword(int Id, string Password)
        {
            string query = "UPDATE \"User\" SET Password = @password WHERE UserID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.Parameters.AddWithValue("@password", Password);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void RemoveUser(int Id)
        {
            string query = "DELETE FROM \"User\" WHERE UserID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", Id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
