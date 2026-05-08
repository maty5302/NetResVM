using System.Data;
using Microsoft.Data.SqlClient;
using DataLayer.Interface;

namespace DataLayer
{
    /// <summary>
    /// This class is responsible for interacting with the Reservation table in the database.
    /// </summary>
    public class ReservationTableDataGateway : IReservationTableDataGateway
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
        /// <param name="reservationId"> ID of the reservations </param>
        /// <returns></returns>
        public DataTable GetReservationById(int reservationId)
        {
            string query = "SELECT * FROM Reservation WHERE ReservationID = @Id";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", reservationId);
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
        /// <param name="userId"> ID of the user </param>
        /// <returns></returns>
        public DataTable GetReservationsByUserId(int userId)
        {
            string query = "SELECT * FROM Reservation WHERE UserID = @Id";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
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
        /// <param name="serverId"> ID of the server </param>
        /// <returns></returns>
        public DataTable GetReservationsByServerId(int serverId)
        {
            string query = "SELECT * FROM Reservation WHERE ServerID = @Id";
            var result = new DataTable();
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", serverId);
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
        /// <param name="labId"> ID of the lab</param>
        public void InsertReservation(int serverId, int userId, DateTime startDate, DateTime endDate, string labId)
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
                    command.Parameters.AddWithValue("@LabID", labId);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Removes an existing reservation in the Reservation table.
        /// </summary>
        /// <param name="reservationId"> ID of the reservation </param>
        public void RemoveReservation(int reservationId)
        {
            string query = "DELETE FROM Reservation WHERE ReservationID = @Id";
            using (var connection = DBConnector.GetConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", reservationId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
