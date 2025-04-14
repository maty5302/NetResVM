using BusinessLayer.DTOs;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    /// <summary>
    /// This class is responsible for mapping DataRow objects to ServerModel and ServerDTO objects.
    /// </summary>
    public static class ServerMapper
    {
        /// <summary>
        /// Maps a DataRow object to a ServerModel object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns>  A ServerModel object with values populated from the DataRow. </returns>
        public static ServerModel Map(DataRow row)
        {
            return new ServerModel
            {
                Id = (int)row["ServerID"],
                ServerType = (string)row["ServerType"],
                Name = (string)row["Name"],
                IpAddress = (string)row["IpAddress"],
                Username = (string)row["Username"],
                Password = (string)row["Password"]
            };
        }

        /// <summary>
        /// Maps a DataRow object to a ServerDTO object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns>  A ServerModel object with values populated from the DataRow. </returns>
        public static ServerDTO MapToDTO(DataRow row)
        {
            return new ServerDTO
            {
                Id = (int)row["ServerID"],
                ServerType = (string)row["ServerType"],
                Name = (string)row["Name"],
                IpAddress = (string)row["IpAddress"],
                Username = (string)row["Username"],
            };
        }

        /// <summary>
        /// Maps a ServerModel object to a ServerDTO object.
        /// </summary>
        /// <param name="dto"> The ServerDTO containing data </param>
        /// <returns>  A ServerModel object with values populated from ServerDTO </returns>
        public static ServerModel ToModel(ServerDTO dto)
        {
            return new ServerModel
            {
                Id = dto.Id,
                ServerType = dto.ServerType,
                Name = dto.Name,
                IpAddress = dto.IpAddress,
                Username = dto.Username,
                Password = string.Empty // Do not populate the password for security reasons
            };
        }
    }
}
