using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.MapperDT
{
    public static class ServerMapper
    {
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
    }
}
