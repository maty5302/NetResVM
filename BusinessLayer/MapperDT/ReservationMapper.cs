using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    public class ReservationMapper
    {
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
