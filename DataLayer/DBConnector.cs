using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    internal class DBConnector
    {
        public static SqlConnectionStringBuilder GetBuilder()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = @"**********";   // update me
            builder.UserID = "******";              // update me
            builder.Password = "********";      // update me //maybe get from file?
            builder.InitialCatalog = "DB_NetResVM";
            return builder;
        }
    }
}
