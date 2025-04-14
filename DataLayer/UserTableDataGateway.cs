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
    /// This class is responsible for interacting with the User table in the database.
    /// </summary>
    public class UserTableDataGateway
    {
        /// <summary>
        /// This method retrieves a user by their username from the database and returns it as a DataTable.
        /// </summary>
        /// <param name="Username"> Username </param>
        /// <returns></returns>
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


        /// <summary>
        /// This method retrieves a user by their ID from the database and returns it as a DataTable.
        /// </summary>
        /// <param name="Id"> ID of user to be retrieved </param>
        /// <returns></returns>
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


        /// <summary>
        /// This method retrieves all users from the database and returns them as a DataTable.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// This method adds a new user to the database.
        /// </summary>
        /// <param name="Username"> Username for account </param>
        /// <param name="Password"> Password for account </param>
        /// <param name="Role"> Role for account (Admin/student)</param>
        /// <param name="AuthorizationType"> Authorization type for account (localhost/vsb)</param>
        /// <param name="Active"> Determines if user is active  </param>
        public void AddUser(string Username, string Password, string Role, string AuthorizationType, int Active)
        {
            string query = "INSERT INTO \"User\" (Username, Password, Role, AuthorizationType, Active) VALUES (@username, @password, @role, @authorizationType, @active)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", Username);
                    command.Parameters.AddWithValue("@password", Password);
                    command.Parameters.AddWithValue("@role", Role);
                    command.Parameters.AddWithValue("@authorizationType", AuthorizationType);
                    command.Parameters.AddWithValue("@active", Active);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// This method adds a new user to the database.
        /// </summary>
        /// <param name="Username"> Username for account </param>        
        /// <param name="Role"> Role for account (Admin/student)</param>
        /// <param name="AuthorizationType"> Authorization type for account (localhost/vsb)</param>
        /// <param name="Active"> Determines if user is active  </param>
        public void AddUser(string Username, string Role, string AuthorizationType, int Active)
        {
            string query = "INSERT INTO \"User\" (Username, Role, AuthorizationType, Active) VALUES (@username, @role, @authorizationType, @active)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", Username);
                    command.Parameters.AddWithValue("@role", Role);
                    command.Parameters.AddWithValue("@authorizationType", AuthorizationType);
                    command.Parameters.AddWithValue("@active", Active);
                    command.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// This method updates the active status of a user in the database.
        /// </summary>
        /// <param name="Id"> ID of User </param>
        /// <param name="Active"> Active status (0/1) </param>
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

        /// <summary>
        /// This method updates the password of a user in the database.
        /// </summary>
        /// <param name="Id"> ID of user to update password for </param>
        /// <param name="Password"> New password </param>
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

        /// <summary>
        /// This method updates the role of a user in the database.
        /// </summary>
        /// <param name="Id"> Id of user to be removed </param>
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
