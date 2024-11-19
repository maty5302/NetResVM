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
            SqlConnectionStringBuilder builder = DBConnector.GetBuilder();
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    //command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = Username;
                    command.Parameters.AddWithValue("@username", Username);
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
