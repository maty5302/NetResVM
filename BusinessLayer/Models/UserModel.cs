using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Models
{
    public class UserModel
    {
        public required int Id { get; set; }
        public required string Username { get; set; }
        public string? Password { get; set; } //password could be null if user is vsb
        public required string Role { get; set; } // admin or student
        public required string AuthorizationType { get; set; } // will be localhost or vsb
        public bool Active { get; set; } // will be true or false
    }
}
