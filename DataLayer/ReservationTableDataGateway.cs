using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class ReservationTableDataGateway
    {
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
