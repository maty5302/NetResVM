using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using DataLayer;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class UserLabOwnershipService
    {
        private readonly UserLabOwnershipTableDataGateway _gateway;
        private static ILogger logger = FileLogger.Instance;
        
        public UserLabOwnershipService()
        {
            _gateway = new UserLabOwnershipTableDataGateway();
        }

        public List<UserLabOwnershipModel>? GetAllUserLabsByUserID(int userId)
        {
            try
            {
                var all = new List<UserLabOwnershipModel>();
                var table = _gateway.GetAllUserLabsByUserID(userId);
                foreach (System.Data.DataRow row in table.Rows)
                {
                    all.Add(UserLabOwnershipMapper.Map(row));
                }
                return all;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return null;
            }
        }

        public (bool,string) InsertUserLabOwnership(UserLabOwnershipModel userLabOwnership)
        {
            try
            {
                var allUserLabs = GetAllUserLabsByUserID(userLabOwnership.UserId);
                if(allUserLabs == null)
                {
                    logger.LogError("Error retrieving user labs.");
                    return (false, "Error retrieving user labs.");
                }
                foreach (var item in allUserLabs)
                {
                    if (item.LabId == userLabOwnership.LabId)
                    { 
                        var message = $"User with ID {userLabOwnership.UserId} already owns lab with ID {userLabOwnership.LabId}.";
                        logger.LogWarning(message);
                        return (false,"You already own this lab.");
                    }
                }
                _gateway.InsertUserLabOwnership(userLabOwnership.UserId,userLabOwnership.LabId, userLabOwnership.ServerId);
                logger.Log($"User with ID {userLabOwnership.UserId} has been assigned ownership of lab with ID {userLabOwnership.LabId}.");
                return (true,"");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return (false,e.Message);
            }
        }

        public bool DeleteUserLabOwnership(UserLabOwnershipModel model)
        {
            try
            {
                _gateway.DeleteUserLabOwnership(model.UserId, model.LabId, model.ServerId);
                logger.Log($"User with ID {model.UserId} has been removed from ownership of lab with ID {model.LabId}.");
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }
        }
    }
}
