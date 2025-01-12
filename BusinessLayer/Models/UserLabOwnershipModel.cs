using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class UserLabOwnershipModel
    {
        public int UserId { get; set; }
        public int ServerId { get; set; }
        public required string LabId { get; set; }
    }
}
