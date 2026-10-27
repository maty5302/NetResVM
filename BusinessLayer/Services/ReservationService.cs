using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using DataLayer;
using SimpleLogger;
using System.Text;
using DataLayer.Interface;

namespace BusinessLayer.Services
{
    /// <summary>
    /// This class is responsible for managing reservations.
    /// </summary>
    public class ReservationService
    {
        private readonly IReservationTableDataGateway _reservationTableDataGateway;
        private static ILogger _logger = FileLogger.Instance;

        public ReservationService(IReservationTableDataGateway reservationTableDataGateway)
        {
            _reservationTableDataGateway = reservationTableDataGateway;
        }
        
        public ReservationService() : this(new ReservationTableDataGateway())
        {
        }

        /// <summary>
        /// Retrieves a list of all reservations in the system.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ReservationModel"/> objects representing the reservations,
        /// returns an empty list if no reservations are found,
        /// or <c>null</c> if error occurs.
        /// </returns>
        public List<ReservationModel>? GetAllReservations()
        {
            try {
                var rows = _reservationTableDataGateway.GetAllReservations();
                var reservations = new List<ReservationModel>();
                foreach (System.Data.DataRow row in rows.Rows)
                {
                    reservations.Add(ReservationMapper.Map(row));
                }
                return reservations;

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a list of reservations for a specific user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier (ID) of the user.</param>
        /// <returns>
        /// A list of <see cref="ReservationModel"/> objects representing the reservations of the user,
        /// returns an empty list if no reservations are found,
        /// or <c>null</c> if error occurs.
        /// </returns>
        public List<ReservationModel>? GetReservationsByUserId(int userId)
        {
            try
            {
                var rows = _reservationTableDataGateway.GetReservationsByUserId(userId);
                var reservations = new List<ReservationModel>();
                foreach (System.Data.DataRow row in rows.Rows)
                {
                    reservations.Add(ReservationMapper.Map(row));
                }

                _logger.Log($"Reservations for user ID:{userId} have been retrieved.");
                return reservations;

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a list of reservations for a specific server by their unique identifier.
        /// </summary>
        /// <param name="serverId">The unique identifier (ID) of the server.</param>
        /// <returns>
        /// A list of <see cref="ReservationModel"/> objects representing the reservations of the server,
        /// returns an empty list if no reservations are found,
        /// or <c>null</c> if error occurs.
        /// </returns>
        public List<ReservationModel>? GetReservationsByServerId(int serverId)
        {
            try
            {
                var rows = _reservationTableDataGateway.GetReservationsByServerId(serverId);
                var reservations = new List<ReservationModel>();
                foreach (System.Data.DataRow row in rows.Rows)
                {
                    reservations.Add(ReservationMapper.Map(row));
                }
                _logger.Log($"Reservations for server ID:{serverId} have been retrieved.");
                return reservations;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Makes a reservation for a specific server or resource.
        /// </summary>
        /// <param name="reservation">The <see cref="ReservationModel"/> object containing the reservation details.</param>
        /// <returns>
        /// <c>true</c> if the reservation was successfully created; otherwise, <c>false</c>.
        /// </returns>
        public bool MakeReservation(ReservationModel reservation)
        {
            if (reservation == null)
            {
                _logger.LogError("Reservation model is null.");
                return false;
            }

            if (reservation.ReservationStart >= reservation.ReservationEnd)
            {
                _logger.LogWarning("Reservation start time must be before end time.");
                return false;
            }

            var reservations = GetAllReservations();
            if (reservations == null)
            {
                _logger.LogError("Error retrieving reservations.");
                return false;
            }
            if (reservations.Any(item => item.ServerId == reservation.ServerId && item.ReservationStart < reservation.ReservationEnd && item.ReservationEnd > reservation.ReservationStart))
            {
                _logger.LogWarning($"Reservation for server ID:{reservation.ServerId} already exists at the time.");
                return false;
            }
            try
            {
                bool inserted = _reservationTableDataGateway.InsertReservation(reservation.ServerId, reservation.UserId, reservation.ReservationStart, reservation.ReservationEnd, reservation.LabId);
                if (!inserted)
                {
                    _logger.LogWarning($"Reservation for server ID:{reservation.ServerId} already exists at the time (atomic DB check).");
                    return false;
                }

                _logger.Log($"Reservation has been made. ServerID:{reservation.ServerId}, UserID:{reservation.UserId}, StartDate:{reservation.ReservationStart}, EndDate:{reservation.ReservationEnd}, LabID:{reservation.LabId}");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes a reservation from the system by its unique identifier without ownership check (for system cleanup).
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the reservation to delete.</param>
        /// <returns>
        /// <c>true</c> if the reservation was successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public bool DeleteReservation(int id)
        {
            try
            {
                _reservationTableDataGateway.RemoveReservation(id);
                _logger.Log($"Reservation has been deleted. ID:{id}");
                return true;
            }
            catch (Exception)
            {
                _logger.LogError($"Error deleting reservation ID:{id}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a reservation from the system by its unique identifier, verifying user ownership or admin privileges.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the reservation to delete.</param>
        /// <param name="userId">The ID of the user requesting deletion.</param>
        /// <param name="isAdmin">Whether the requesting user has administrator privileges.</param>
        /// <returns>
        /// <c>true</c> if the reservation was successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public bool DeleteReservation(int id, int userId, bool isAdmin = false)
        {
            try
            {
                var reservation = _reservationTableDataGateway.GetReservationById(id);
                if (reservation == null || reservation.Rows.Count == 0)
                {
                    _logger.LogWarning($"Reservation ID:{id} not found.");
                    return false;
                }

                int ownerId = (int)reservation.Rows[0]["UserID"];
                if (!isAdmin && ownerId != userId)
                {
                    _logger.LogWarning($"User ID:{userId} is not authorized to delete reservation ID:{id}.");
                    return false;
                }

                return DeleteReservation(id);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error deleting reservation ID:{id}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the reservation details to the system by its unique identifier.
        /// </summary>
        /// <param name="reservationId">The unique identifier (ID) of the reservation to save.</param>
        /// <returns>
        /// A <see cref="StringBuilder"/> object containing a message indicating the result of the save operation, or <c>null</c> if the reservation is not found.
        /// </returns>
        public StringBuilder? SaveReservation(int reservationId)
        {
            var reservation = _reservationTableDataGateway.GetReservationById(reservationId);
            if (reservation.Rows.Count == 0)
            {
                _logger.LogError($"Reservation ID:{reservationId} not found.");
                return null;
            }
            var reservationModel = ReservationMapper.Map(reservation.Rows[0]);
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR").AppendLine("VERSION:2.0").AppendLine("BEGIN:VEVENT").AppendLine($"SUMMARY:Reservation of lab")
                .AppendLine($"DTSTART:{reservationModel.ReservationStart:yyyyMMddTHHmmss}").AppendLine($"DTEND:{reservationModel.ReservationEnd:yyyyMMddTHHmmss}")
                .AppendLine("LOCATION:").AppendLine("DESCRIPTION:").AppendLine("END:VEVENT").AppendLine("END:VCALENDAR");

            return sb;
        }
    }
}
