using DataLayer.Interface;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DataLayer
{
    /// <summary>
    /// This class is responsible for interacting with the User table in the database.
    /// </summary>
    public class UserTableDataGateway : IUserTableDataGateway
    {
        /// <summary>
        /// This method retrieves a user by their username from the database and returns it as a DataTable.
        /// </summary>
        /// <param name="username"> Username </param>
        /// <returns></returns>
        public DataTable GetUserByUsername(string username)
        {
            string query = "SELECT * FROM \"User\" WHERE Username=@username";
            var result = new DataTable();
           
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
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
        /// <param name="id"> ID of user to be retrieved </param>
        /// <returns></returns>
        public DataTable GetUserById(int id)
        {
            string query = "SELECT * FROM \"User\" WHERE UserID = @Id";
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
        /// <param name="username"> Username for account </param>
        /// <param name="password"> Password for account </param>
        /// <param name="role"> Role for account (Admin/student)</param>
        /// <param name="authorizationType"> Authorization type for account (localhost/vsb)</param>
        /// <param name="active"> Determines if user is active  </param>
        public void AddUser(string username, string password, string role, string authorizationType, int active)
        {
            string query = "INSERT INTO \"User\" (Username, Password, Role, AuthorizationType, Active) VALUES (@username, @password, @role, @authorizationType, @active)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@role", role);
                    command.Parameters.AddWithValue("@authorizationType", authorizationType);
                    command.Parameters.AddWithValue("@active", active);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// This method adds a new user to the database.
        /// </summary>
        /// <param name="username"> Username for account </param>        
        /// <param name="role"> Role for account (Admin/student)</param>
        /// <param name="authorizationType"> Authorization type for account (localhost/vsb)</param>
        /// <param name="active"> Determines if user is active  </param>
        public void AddUser(string username, string role, string authorizationType, int active)
        {
            string query = "INSERT INTO \"User\" (Username, Role, AuthorizationType, Active) VALUES (@username, @role, @authorizationType, @active)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@role", role);
                    command.Parameters.AddWithValue("@authorizationType", authorizationType);
                    command.Parameters.AddWithValue("@active", active);
                    command.ExecuteNonQuery();
                }
            }
        }


        /// <summary>
        /// This method updates the active status of a user in the database.
        /// </summary>
        /// <param name="id"> ID of User </param>
        /// <param name="active"> Active status (0/1) </param>
        public void UpdateUserActive(int id, int active)
        {
            string query = "UPDATE \"User\" SET Active = @active WHERE UserID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@active", active);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// This method updates the password of a user in the database.
        /// </summary>
        /// <param name="id"> ID of user to update password for </param>
        /// <param name="password"> New password </param>
        public void UpdateUserPassword(int id, string password)
        {
            string query = "UPDATE \"User\" SET Password = @password WHERE UserID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@password", password);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// This method updates the role of a user in the database.
        /// </summary>
        /// <param name="id"> Id of user to be removed </param>
        public void RemoveUser(int id)
        {
            string query = "DELETE FROM \"User\" WHERE UserID = @Id";
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
