using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    public class UserMapper
    {
        public static UserModel Map(DataRow row)
        {
            return new UserModel
            {
                Id = (int)row["UserID"],
                Username = (string)row["Username"],
                Password = (string)row["Password"],
                Role = (string)row["Role"]
            };
        }
    }
}
