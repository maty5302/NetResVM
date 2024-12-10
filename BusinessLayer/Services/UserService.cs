using DataLayer;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.MapperDT;

namespace BusinessLayer.Services
{
    public class UserService
    {
        private readonly UserTableDataGateway _userTableDataGateway;

        public UserService()
        {
            _userTableDataGateway = new UserTableDataGateway();
        }

        private UserModel? GetUser(string Username)
        {
            var row = _userTableDataGateway.GetUserByUsername(Username);
            if(row.Rows.Count>0)
            {
                return UserMapper.Map(row.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public bool ValidateCredentials(string Username, string Password)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                return user.Password == Password;
            }
            return false;
        }

        public string GetUsername(int id)
        {
            var user = _userTableDataGateway.GetUserById(id);
            if (user.Rows.Count > 0)
            {
                return (string)user.Rows[0]["Username"];
            }
            return "";
        }

        public int GetUserId(string Username)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                return user.Id;
            }
            return -1;
        }

        public string GetRole(string Username)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                return user.Role;
            }
            return "";
        }
    }
}
