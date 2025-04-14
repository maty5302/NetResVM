using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    /// <summary>
    /// This class is responsible for interacting with the UserLabOwnership table in the database.
    /// </summary>
    public class UserLabOwnershipTableDataGateway
    {
        /// <summary>
        /// Retrieves all user lab ownership records from the UserLabOwnership table.
        /// </summary>
        /// <param name="userID"> ID of a user </param>
        /// <returns></returns>
        public DataTable GetAllUserLabsByUserID(int userID)
        {
            string query = "SELECT * FROM UserLabOwnership WHERE UserID = @UserID";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userID);
                    using (var reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Retrieves all user lab ownership records from the UserLabOwnership table by LabID.
        /// </summary>
        /// <param name="labID"> ID of a lab </param>
        /// <returns></returns>
        public DataTable GetAllUserLabsByLabID(string labID)
        {
            string query = "SELECT * FROM UserLabOwnership WHERE LabID = @LabID";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@LabID", labID);
                    using (var reader = command.ExecuteReader())
                    {
                        result.Load(reader);
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// Inserts a new user lab ownership record into the UserLabOwnership table.
        /// </summary>
        /// <param name="userID"> ID of a user </param>
        /// <param name="labID"> ID of a lab </param>
        /// <param name="serverID"> ID of a server </param>
        public void InsertUserLabOwnership(int userID, string labID, int serverID)
        {
            string query = "INSERT INTO UserLabOwnership (UserID, LabID, ServerID) VALUES (@UserID, @LabID, @ServerID)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@LabID", labID);
                    command.Parameters.AddWithValue("@ServerID", serverID);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a user lab ownership record from the UserLabOwnership table.
        /// </summary>
        /// <param name="userID"> ID of a user </param>
        /// <param name="labID"> ID of a lab </param>
        /// <param name="serverID"> ID of a server </param>
        public void DeleteUserLabOwnership(int userID, string labID, int serverID)
        {
            string query = "DELETE FROM UserLabOwnership WHERE UserID = @UserID AND LabID = @LabID AND ServerID = @ServerID";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@LabID", labID);
                    command.Parameters.AddWithValue("@ServerID", serverID);
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
