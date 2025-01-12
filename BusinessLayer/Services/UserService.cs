using DataLayer;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.MapperDT;
using SimpleLogger;

namespace BusinessLayer.Services
{
    public class UserService
    {
        private readonly UserTableDataGateway _userTableDataGateway;
        private static ILogger logger = FileLogger.Instance;

        public UserService()
        {
            _userTableDataGateway = new UserTableDataGateway();
        }

        private UserModel? GetUser(string Username)
        {
            try
            {
                var row = _userTableDataGateway.GetUserByUsername(Username);
                if (row.Rows.Count > 0)
                {
                    return UserMapper.Map(row.Rows[0]);
                }
                else
                {
                    logger.LogWarning($"User with username {Username} not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }

        public bool ValidateCredentials(string Username, string Password)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                if(user.Password == Password)
                {
                    return true;
                }
                logger.LogWarning($"Invalid password for user with username {Username}.");
                return false;
            }
            logger.LogWarning($"User with username {Username} not found.");
            return false;
        }

        public string GetUsername(int id)
        {
            try
            {
                var user = _userTableDataGateway.GetUserById(id);
                if (user.Rows.Count > 0)
                {
                    return (string)user.Rows[0]["Username"];
                }
                logger.LogWarning($"User with ID {id} not found.");
                return "";
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return "";
            }
        }

        public int GetUserId(string Username)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                return user.Id;
            }
            logger.LogWarning($"User with username {Username} not found.");
            return -1;
        }

        public string GetRole(string Username)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                return user.Role;
            }
            logger.LogWarning($"User with username {Username} not found.");
            return "";
        }
    }
}
