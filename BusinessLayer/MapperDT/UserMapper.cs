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
    /// <summary>
    /// This class is responsible for mapping DataRow objects to UserModel and UserDTO objects.
    /// </summary>
    public class UserMapper
    {
        /// <summary>
        /// Maps a DataRow object to a UserModel object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns> A UserModel object with values populated from the DataRow. </returns>
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

        /// <summary>
        /// Maps a DataRow object to a UserDTO object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns> A UserDTO object with values populated from the DataRow. </returns>
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
