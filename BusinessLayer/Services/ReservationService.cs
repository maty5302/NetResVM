using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using DataLayer;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class ReservationService
    {
        private readonly ReservationTableDataGateway _reservationTableDataGateway;
        private static ILogger logger = FileLogger.Instance;

        public ReservationService()
        {
            _reservationTableDataGateway = new ReservationTableDataGateway();
        }

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
                logger.LogError(e.Message);
                return null;
            }
        }


        public List<ReservationModel> GetReservationsByUserId(int userId)
        {
            try
            {
                var rows = _reservationTableDataGateway.GetReservationsByUserId(userId);
                var reservations = new List<ReservationModel>();
                foreach (System.Data.DataRow row in rows.Rows)
                {
                    reservations.Add(ReservationMapper.Map(row));
                }

                logger.Log($"Reservations for user ID:{userId} have been retrieved.");
                return reservations;

            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return null;
            }
        }

        public List<ReservationModel> GetReservationsByServerId(int serverId)
        {
            try
            {
                var rows = _reservationTableDataGateway.GetReservationsByServerId(serverId);
                var reservations = new List<ReservationModel>();
                foreach (System.Data.DataRow row in rows.Rows)
                {
                    reservations.Add(ReservationMapper.Map(row));
                }
                logger.Log($"Reservations for server ID:{serverId} have been retrieved.");
                return reservations;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return null;
            }
        }

        public bool MakeReservation(ReservationModel reservation)
        {
            try
            {
                foreach (var item in GetAllReservations())
                {
                    // Check if reservation already exists for the server at the time or is overlapping
                    if (item.ServerId == reservation.ServerId && item.ReservationStart < reservation.ReservationEnd && item.ReservationEnd > reservation.ReservationStart)
                    {
                        logger.LogWarning($"Reservation for server ID:{reservation.ServerId} already exists at the time.");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }
            try
            {
                _reservationTableDataGateway.InsertReservation(reservation.ServerId, reservation.UserId, reservation.ReservationStart, reservation.ReservationEnd, reservation.LabId);
                logger.Log($"Reservation has been made. ServerID:{reservation.ServerId}, UserID:{reservation.UserId}, StartDate:{reservation.ReservationStart}, EndDate:{reservation.ReservationEnd}, LabID:{reservation.LabId}");
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                return false;
            }
        }

        public bool DeleteReservation(int id)
        {
            try
            {
                _reservationTableDataGateway.RemoveReservation(id);
                logger.Log($"Reservation has been deleted. ID:{id}");
                return true;
            }
            catch (Exception)
            {
                logger.LogError($"Error deleting reservation ID:{id}");
                return false;
            }
        }

        public StringBuilder SaveReservation(int reservationId)
        {
            var reservation = _reservationTableDataGateway.GetReservationById(reservationId);
            var reservationModel = ReservationMapper.Map(reservation.Rows[0]);
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR").AppendLine("VERSION:2.0").AppendLine("BEGIN:VEVENT").AppendLine($"SUMMARY:Reservation of lab")
                .AppendLine($"DTSTART:{reservationModel.ReservationStart:yyyyMMddTHHmmss}").AppendLine($"DTEND:{reservationModel.ReservationEnd:yyyyMMddTHHmmss}")
                .AppendLine("LOCATION:").AppendLine("DESCRIPTION:").AppendLine("END:VEVENT").AppendLine("END:VCALENDAR");

            return sb;
        }
    }
}
