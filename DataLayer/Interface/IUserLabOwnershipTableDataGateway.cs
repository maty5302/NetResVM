using System.Data;

namespace DataLayer.Interface;

/// <summary>
/// Defines methods for accessing and managing user-lab ownership records in the data store.
/// </summary>
/// <remarks>This interface provides operations to query, insert, and delete associations between users and labs.
/// Implementations are responsible for handling the underlying data access logic. Methods typically return results as
/// DataTable objects for compatibility with data-driven components.</remarks>
public interface IUserLabOwnershipTableDataGateway
{
    /// <summary>
    /// Retrieves all laboratory records associated with the specified user identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose laboratory records are to be retrieved. Must be a positive integer.</param>
    /// <returns>A DataTable containing the laboratory records for the specified user. The table will be empty if the user has no
    /// associated laboratories.</returns>
    DataTable GetAllUserLabsByUserId(int userId);

    /// <summary>
    /// Retrieves a data table containing all user lab records associated with the specified lab identifier.
    /// </summary>
    /// <param name="labId">The unique identifier of the lab for which to retrieve user lab records. Cannot be null or empty.</param>
    /// <returns>A DataTable containing user lab records for the specified lab. The table will be empty if no records are found.</returns>
    DataTable GetAllUserLabsByLabId(string labId);

    /// <summary>
    /// Assigns ownership of a laboratory to a user on a specified server.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to whom the laboratory ownership will be assigned.</param>
    /// <param name="labId">The unique identifier of the laboratory to assign.</param>
    /// <param name="serverId">The unique identifier of the server where the laboratory is located.</param>
    void InsertUserLabOwnership(int userId, string labId, int serverId);

    /// <summary>
    /// Removes the ownership association between a specified user and laboratory on a given server.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose ownership is to be removed.</param>
    /// <param name="labId">The unique identifier of the laboratory from which the user's ownership will be deleted.</param>
    /// <param name="serverId">The unique identifier of the server hosting the laboratory.</param>
    void DeleteUserLabOwnership(int userId, string labId, int serverId);
}