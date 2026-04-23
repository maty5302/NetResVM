using System.Data;

namespace DataLayer.Interface;

public interface IUserLabOwnershipTableDataGateway
{
        DataTable GetAllUserLabsByUserId(int userId);
        DataTable GetAllUserLabsByLabId(string labId);
        void InsertUserLabOwnership(int userId, string labId, int serverId);
        void DeleteUserLabOwnership(int userId, string labId, int serverId);
}