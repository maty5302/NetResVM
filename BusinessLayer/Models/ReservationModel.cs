using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class ReservationModel
    {
        public int Id { get; set; }
        public required int UserId { get; set; }
        public required int ServerId { get; set; }
        public required string LabId { get; set; }
        public required DateTime ReservationStart { get; set; }
        public required DateTime ReservationEnd { get; set; }

    }
}
