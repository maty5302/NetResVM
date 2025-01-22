using BusinessLayer.DTOs;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
                Password = row["Password"] is DBNull ? null : row["Password"].ToString(),
                Role = (string)row["Role"],
                AuthorizationType = (string)row["AuthorizationType"],
                Active = row["Active"] is 1 ? true : false
            };
        }

        public static UserDTO MapToDTO(DataRow row)
        {
            return new UserDTO
            {
                Id = (int)row["UserID"],
                Username = (string)row["Username"],
                Role = (string)row["Role"],
                AuthorizationType = (string)row["AuthorizationType"],
                Active = row["Active"] is 1 ? true : false
            };
        }
    }
}
