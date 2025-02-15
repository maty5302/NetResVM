using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class BackupRecord
    {
        public string ServerType { get; set; }
        public string LabId { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
