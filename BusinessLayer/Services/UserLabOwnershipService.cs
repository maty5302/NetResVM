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

        public bool InsertUserLabOwnership(UserLabOwnershipModel userLabOwnership)
        {
            try
            {
                foreach (var item in GetAllUserLabsByUserID(userLabOwnership.UserId))
                {
                    if (item.LabId == userLabOwnership.LabId)
                    {
                        logger.LogWarning($"User with ID {userLabOwnership.UserId} already owns lab with ID {userLabOwnership.LabId}.");
                        return false;
                    }
                }
                _gateway.InsertUserLabOwnership(userLabOwnership.UserId,userLabOwnership.LabId, userLabOwnership.ServerId);
                logger.Log($"User with ID {userLabOwnership.UserId} has been assigned ownership of lab with ID {userLabOwnership.LabId}.");
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
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
