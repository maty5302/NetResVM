using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    public class UserLabOwnershipMapper
    {
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
