using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using DataLayer;
using SimpleLogger;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Service class for managing user lab ownership.
    /// </summary>
    public class UserLabOwnershipService
    {
        private readonly UserLabOwnershipTableDataGateway _gateway;
        private static ILogger _logger = FileLogger.Instance;
        
        public UserLabOwnershipService()
        {
            _gateway = new UserLabOwnershipTableDataGateway();
        }

        /// <summary>
        /// Retrieves all lab ownership records associated with a specific user ID.
        /// </summary>
        /// <param name="userId">The unique identifier (ID) of the user.</param>
        /// <returns>
        /// A list of <see cref="UserLabOwnershipModel"/> objects representing the labs owned by the user,
        /// or <c>null</c> if no records are found.
        /// </returns>
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
                _logger.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks whether a lab is already owned and whether it is owned by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user to check ownership for.</param>
        /// <param name="labId">The unique identifier of the lab.</param>
        /// <returns>
        /// A tuple containing two boolean values:
        /// <c>owned</c> – <c>true</c> if the lab is already owned by any user;
        /// <c>userOwns</c> – <c>true</c> if the lab is owned by the specified user.
        /// </returns>
        public (bool owned,bool userOwns) IsLabAlreadyOwned(int userId, string labId)
        {
            try
            {
                var all = _gateway.GetAllUserLabsByLabID(labId);
                if (all.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in all.Rows)
                    {
                        if ((int)row["UserID"] == userId)
                        {
                            return (true, true);
                        }
                    }
                    return (true,false);
                }
                return (false,false);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return (false, false);
            }
        }

        /// <summary>
        /// Inserts a new user-lab ownership record into the system.
        /// </summary>
        /// <param name="userLabOwnership">The <see cref="UserLabOwnershipModel"/> object containing ownership details.</param>
        /// <returns>
        /// A tuple containing:
        /// <c>bool</c> – <c>true</c> if the insertion was successful; otherwise, <c>false</c>.
        /// <c>string</c> – a message describing the result (e.g., success confirmation or error details).
        /// </returns>
        public (bool,string) InsertUserLabOwnership(UserLabOwnershipModel userLabOwnership)
        {
            try
            {
                var allUserLabs = GetAllUserLabsByUserID(userLabOwnership.UserId);
                if(allUserLabs == null)
                {
                    _logger.LogError("Error retrieving user labs.");
                    return (false, "Error retrieving user labs.");
                }
                foreach (var item in allUserLabs)
                {
                    if (item.LabId == userLabOwnership.LabId)
                    { 
                        var message = $"User with ID {userLabOwnership.UserId} already owns lab with ID {userLabOwnership.LabId}.";
                        _logger.LogWarning(message);
                        return (false,"You already own this lab.");
                    }
                }
                _gateway.InsertUserLabOwnership(userLabOwnership.UserId,userLabOwnership.LabId, userLabOwnership.ServerId);
                _logger.Log($"User with ID {userLabOwnership.UserId} has been assigned ownership of lab with ID {userLabOwnership.LabId}.");
                return (true,"");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return (false,e.Message);
            }
        }

        /// <summary>
        /// Deletes a user-lab ownership record from the system.
        /// </summary>
        /// <param name="model">The <see cref="UserLabOwnershipModel"/> representing the ownership to be removed.</param>
        /// <returns>
        /// <c>true</c> if the ownership record was successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public bool DeleteUserLabOwnership(UserLabOwnershipModel model)
        {
            try
            {
                _gateway.DeleteUserLabOwnership(model.UserId, model.LabId, model.ServerId);
                _logger.Log($"User with ID {model.UserId} has been removed from ownership of lab with ID {model.LabId}.");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return false;
            }
        }
    }
}
