using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    public class UserMapper
    {

        private static bool GetActiveRow(DataRow row)
        {
            if (row["Active"].ToString() == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static UserModel Map(DataRow row)
        {
            return new UserModel
            {
                Id = (int)row["UserID"],
                Username = (string)row["Username"],
                Password = (string)row["Password"],
                Role = (string)row["Role"],
                Active = GetActiveRow(row)
            };
        }
    }
}
