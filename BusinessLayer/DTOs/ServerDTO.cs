using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class ServerDTO
    {
        public required int Id { get; set; }
        public required string ServerType { get; set; }
        public required string Name { get; set; }
        public required string IpAddress { get; set; }
        public required string Username { get; set; }
    }
}
