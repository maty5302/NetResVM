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
    /// This class is responsible for mapping DataRow objects to UserLabOwnershipModel objects.
    /// </summary>
    public class UserLabOwnershipMapper
    {
        /// <summary>
        /// Maps a DataRow object to a UserLabOwnershipModel object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns>  A UserLabOwnershipModel object with values populated from the DataRow. </returns>
        public static UserLabOwnershipModel Map(DataRow row)
        {
            return new UserLabOwnershipModel
            {
                UserId = (int)row["UserID"],
                LabId = (string)row["LabID"],
                ServerId = (int)row["ServerID"]
            };
        }
    }
}
