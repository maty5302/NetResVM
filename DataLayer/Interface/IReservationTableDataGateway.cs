using System.Data;

namespace DataLayer.Interface;

/// <summary>
/// Defines methods for accessing and managing reservation data in a tabular format.
/// </summary>
/// <remarks>This interface abstracts the data access operations for reservations, enabling retrieval, insertion,
/// and removal of reservation records. Implementations are responsible for interacting with the underlying data store.
/// All methods return or accept data in types compatible with ADO.NET for integration with database
/// operations.</remarks>
public interface IReservationTableDataGateway
{
    /// <summary>
    /// Retrieves all reservations from the data source.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> containing all reservation records. The table will be empty if no reservations exist.</returns>
    DataTable GetAllReservations();

    /// <summary>
    /// Retrieves reservation details for the specified reservation identifier.
    /// </summary>
    /// <param name="reservationId">The unique identifier of the reservation to retrieve. Must be a positive integer.</param>
    /// <returns>A DataTable containing the reservation details if found; otherwise, an empty DataTable.</returns>
    DataTable GetReservationById(int reservationId);
    
    /// <summary>
    /// Retrieves all reservations associated with the specified user identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose reservations are to be retrieved. Must be a positive integer.</param>
    /// <returns>A DataTable containing the reservations for the specified user. The table will be empty if the user has no
    /// reservations.</returns>
    DataTable GetReservationsByUserId(int userId);

    /// <summary>
    /// Retrieves a table containing all reservations associated with the specified server identifier.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server for which to retrieve reservations.</param>
    /// <returns>A DataTable containing reservation records for the specified server. The table will be empty if no reservations
    /// are found.</returns>
    DataTable GetReservationsByServerId(int serverId);

    /// <summary>
    /// Adds a new server reservation for the specified user within the specified time range and for the specified lab.
    /// </summary>
    /// <param name="serverId">The identifier of the server to be reserved.</param>
    /// <param name="userId">The identifier of the user for whom the reservation is intended.</param>
    /// <param name="startDate">The date and time the reservation begins.</param>
    /// <param name="endDate">The end date and time of the reservation. Must be later than <paramref name="startDate"/>.</param>
    /// <param name="labId">The identifier of the lab in which the server is reserved. Cannot be null or an empty string.</param>
    void InsertReservation(int serverId, int userId, DateTime startDate, DateTime endDate, string labId);
    
    /// <summary>
    /// Removes the reservation with the specified identifier.
    /// </summary>
    /// <param name="reservationId">The unique identifier of the reservation to remove. Must correspond to an existing reservation.</param>
    void RemoveReservation(int reservationId);
}