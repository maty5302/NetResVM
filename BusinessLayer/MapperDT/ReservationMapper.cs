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
    /// This class is responsible for mapping DataRow objects to ReservationModel objects.
    /// </summary>
    public class ReservationMapper
    {
        /// <summary>
        /// Maps a DataRow object to a ReservationModel object.
        /// </summary>
        /// <param name="row"> The DataRow containing data from the database. </param>
        /// <returns>  A ReservationModel object with values populated from the DataRow. </returns>
        public static ReservationModel Map(DataRow row)
        {
            return new ReservationModel
            {
                Id = (int)row["ReservationID"],
                ServerId = (int)row["ServerID"],
                UserId = (int)row["UserID"],
                LabId = (string)row["LabID"],
                ReservationStart = (DateTime)row["StartDate"],
                ReservationEnd = (DateTime)row["EndDate"]
            };
        }
    }
}
