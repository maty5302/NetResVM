using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class DatabaseConfigurationException : Exception
    {
        public DatabaseConfigurationException(string message) : base(message)
        {
        }
    }
}
