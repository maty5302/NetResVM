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
    /// This class is responsible for interacting with the Reservation table in the database.
    /// </summary>
    public class ReservationTableDataGateway
    {
        /// <summary>
        /// Retrieves all reservations from the Reservation table.
        /// </summary>
        /// <returns></returns>
        public DataTable GetAllReservations()
        {
            string query = "SELECT * FROM Reservation";
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
        /// Retrieves a reservation by its ID from the Reservation table.
        /// </summary>
        /// <param name="id"> ID of the reservations </param>
        /// <returns></returns>
        public DataTable GetReservationById(int id)
        {
            string query = "SELECT * FROM Reservation WHERE ReservationID = @Id";
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
        /// Retrieves all reservations made by a specific user from the Reservation table.
        /// </summary>
        /// <param name="id"> ID of the user </param>
        /// <returns></returns>
        public DataTable GetReservationsByUserId(int id)
        {
            string query = "SELECT * FROM Reservation WHERE UserID = @Id";
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
        /// Retrieves all reservations made for a specific server from the Reservation table.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <returns></returns>
        public DataTable GetReservationsByServerId(int id)
        {
            string query = "SELECT * FROM Reservation WHERE ServerID = @Id";
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
        /// Inserts a new reservation into the Reservation table.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="userId"> ID of the user </param>
        /// <param name="startDate"> Date and time of starting reservation </param>
        /// <param name="endDate"> Date and time of stopping reservation </param>
        /// <param name="labID"> ID of the lab</param>
        public void InsertReservation(int serverId, int userId, DateTime startDate, DateTime endDate, string labID)
        {
            string query = "INSERT INTO Reservation (ServerID, UserID, StartDate, EndDate, LabID) VALUES (@ServerID, @UserID, @StartDate, @EndDate, @LabID)";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ServerID", serverId);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);
                    command.Parameters.AddWithValue("@LabID", labID);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Removes an existing reservation in the Reservation table.
        /// </summary>
        /// <param name="id"> ID of the reservation </param>
        public void RemoveReservation(int id)
        {
            string query = "DELETE FROM Reservation WHERE ReservationID = @Id";
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
