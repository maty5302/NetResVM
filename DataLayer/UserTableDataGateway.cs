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
    }
}
