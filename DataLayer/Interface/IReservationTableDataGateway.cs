using System.Data;

namespace DataLayer.Interface;

public interface IReservationTableDataGateway
{
    DataTable GetAllReservations();
    DataTable GetReservationById(int reservationId);
    DataTable GetReservationsByUserId(int userId);
    DataTable GetReservationsByServerId(int serverId);
    void InsertReservation(int serverId, int userId, DateTime startDate, DateTime endDate, string labId);
    void RemoveReservation(int reservationId);
}