using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Interface;

namespace DataLayer
{
    /// <summary>
    /// This class is responsible for interacting with the UserLabOwnership table in the database.
    /// </summary>
    public class UserLabOwnershipTableDataGateway : IUserLabOwnershipTableDataGateway
    {
        /// <summary>
        /// Retrieves all user lab ownership records from the UserLabOwnership table.
        /// </summary>
        /// <param name="userId"> ID of a user </param>
        /// <returns></returns>
        public DataTable GetAllUserLabsByUserId(int userId)
        {
            string query = "SELECT * FROM UserLabOwnership WHERE UserID = @UserID";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userId);
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
        /// <param name="labId"> ID of a lab </param>
        /// <returns></returns>
        public DataTable GetAllUserLabsByLabId(string labId)
        {
            string query = "SELECT * FROM UserLabOwnership WHERE LabID = @LabID";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@LabID", labId);
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
        /// <param name="userId"> ID of a user </param>
        /// <param name="labId"> ID of a lab </param>
        /// <param name="serverId"> ID of a server </param>
        public void InsertUserLabOwnership(int userId, string labId, int serverId)
        {
            string query = "INSERT INTO UserLabOwnership (UserID, LabID, ServerID) VALUES (@UserID, @LabID, @ServerID)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@LabID", labId);
                    command.Parameters.AddWithValue("@ServerID", serverId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Deletes a user lab ownership record from the UserLabOwnership table.
        /// </summary>
        /// <param name="userId"> ID of a user </param>
        /// <param name="labId"> ID of a lab </param>
        /// <param name="serverId"> ID of a server </param>
        public void DeleteUserLabOwnership(int userId, string labId, int serverId)
        {
            string query = "DELETE FROM UserLabOwnership WHERE UserID = @UserID AND LabID = @LabID AND ServerID = @ServerID";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@LabID", labId);
                    command.Parameters.AddWithValue("@ServerID", serverId);
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
