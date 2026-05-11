using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Models;
using System.Data;

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
            //to remain compatible with old database, we need to parse the server type from string to enum
            string serverType = (string)row["ServerType"];
            System.Enum.TryParse<PlatformType>(serverType, true, out var platformType);

            return new ServerModel
            {
                Id = (int)row["ServerID"],
                Platform = platformType,
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
            string serverType = (string)row["ServerType"];
            System.Enum.TryParse<PlatformType>(serverType, true, out var platformType);

            return new ServerDTO
            {
                Id = (int)row["ServerID"],
                Platform = platformType,
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
                Platform = dto.Platform,
                Name = dto.Name,
                IpAddress = dto.IpAddress,
                Username = dto.Username,
                Password = string.Empty // Do not populate the password for security reasons
            };
        }
    }
}
