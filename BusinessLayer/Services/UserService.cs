using DataLayer;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.MapperDT;
using SimpleLogger;
using System.Reflection.Metadata.Ecma335;

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

        private UserModel? GetUser(int Id)
        {
            try
            {
                var row = _userTableDataGateway.GetUserById(Id);
                if (row.Rows.Count > 0)
                {
                    return UserMapper.Map(row.Rows[0]);
                }
                else
                {
                    logger.LogWarning($"User with ID {Id} not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }

        public List<UserModel> GetAllUsersInfo()
        {
            try
            {
                var all = new List<UserModel>();
                var table = _userTableDataGateway.GetAllUsers();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    var user = UserMapper.Map(row);
                    user.Password = "";
                    all.Add(user);
                }
                return all;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return new List<UserModel>();
            }

        }

        public bool ValidateCredentials(string Username, string Password)
        {
            var user = GetUser(Username);
            if (user != null)
            {
                if(user.Password == Password && user.Active)
                {
                    return true;
                }
                else if(!user.Active)
                {
                    logger.LogWarning($"User {Username} is not marked as Active denying access..");
                    return false;
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

        public bool AddUser(string Username, string Password, string Role /*,string AuthorizationType*/, bool Active)
        {
            try
            {
                if(GetUser(Username) != null)
                {
                    logger.LogWarning($"User with username {Username} already exists.");
                    return false;
                }
                int ActiveInt = Active ? 1 : 0;
                _userTableDataGateway.AddUser(Username, Password, Role, ActiveInt);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }

        public bool UpdateUser(int Id, bool Active)
        {
            try
            {
                var user = _userTableDataGateway.GetUserById(Id);
                if (user.Rows.Count == 0)
                {
                    logger.LogWarning($"User with ID {Id} not found.");
                    return false;
                }
                int ActiveInt = Active ? 1 : 0;
                _userTableDataGateway.UpdateUserActive(Id, ActiveInt);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }

        public bool UpdateUser(int Id, string Password)
        {
            try
            {
                var user = _userTableDataGateway.GetUserById(Id);
                if (user.Rows.Count == 0)
                {
                    logger.LogWarning($"User with ID {Id} not found.");
                    return false;
                }
                _userTableDataGateway.UpdateUserPassword(Id, Password);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }

        public bool RemoveUser(int Id)
        {
            try
            {
                var user = GetUser(Id);
                if (user == null)
                {
                    logger.LogWarning($"User with ID {Id} not found.");
                    return false;
                }                
                _userTableDataGateway.RemoveUser(user.Id);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
