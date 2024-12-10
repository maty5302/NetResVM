using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class ReservationService
    {
        private readonly ReservationTableDataGateway _reservationTableDataGateway;

        public ReservationService()
        {
            _reservationTableDataGateway = new ReservationTableDataGateway();
        }

        public List<ReservationModel> GetAllReservations()
        {
            var rows = _reservationTableDataGateway.GetAllReservations();
            var reservations = new List<ReservationModel>();
            foreach (System.Data.DataRow row in rows.Rows)
            {
                reservations.Add(ReservationMapper.Map(row));
            }
            return reservations;

        }

        public List<ReservationModel> GetReservationsByUserId(int userId)
        {
            var rows = _reservationTableDataGateway.GetReservationsByUserId(userId);
            var reservations = new List<ReservationModel>();
            foreach (System.Data.DataRow row in rows.Rows)
            {
                reservations.Add(ReservationMapper.Map(row));
            }
            return reservations;
        }

        public List<ReservationModel> GetReservationsByServerId(int serverId)
        {
            var rows = _reservationTableDataGateway.GetReservationsByServerId(serverId);
            var reservations = new List<ReservationModel>();
            foreach (System.Data.DataRow row in rows.Rows)
            {
                reservations.Add(ReservationMapper.Map(row));
            }
            return reservations;
        }

        public bool MakeReservation(ReservationModel reservation)
        {
            foreach (var item in GetAllReservations())
            {
                if (item.ServerId == reservation.ServerId && item.ReservationStart == reservation.ReservationEnd)
                {
                    return false;
                }
            }
            _reservationTableDataGateway.InsertReservation(reservation.ServerId, reservation.UserId, reservation.ReservationStart, reservation.ReservationEnd,reservation.LabId);
            return true;
        }

        public bool DeleteReservation(int id)
        {
            try
            {
                _reservationTableDataGateway.RemoveReservation(id);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
