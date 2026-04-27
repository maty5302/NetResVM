using System.Data;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Interface;
using Moq;

namespace NetResVM.UnitTests;

public class ReservationServiceTests
{
    private readonly Mock<IReservationTableDataGateway> _mockDataGateway;
    private readonly ReservationService _reservationService;
    
    public ReservationServiceTests()
    {
        _mockDataGateway = new Mock<IReservationTableDataGateway>();
        _reservationService = new ReservationService(_mockDataGateway.Object);
    }
    
    #region GetAllReservations Tests

    [Fact]
    public void GetAllReservations_ShouldReturnAllReservations()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ReservationID", typeof(int));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("UserID", typeof(int));   
        datatable.Columns.Add("StartDate", typeof(DateTime));
        datatable.Columns.Add("EndDate", typeof(DateTime));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Rows.Add(1, 1, 1, DateTime.Now, DateTime.Now.AddHours(1), "1");
        _mockDataGateway.Setup(m => m.GetAllReservations()).Returns(datatable);

        var result = _reservationService.GetAllReservations();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].ServerId);
        Assert.Equal(1, result[0].UserId);
        Assert.Equal("1", result[0].LabId);
        Assert.Equal(datatable.Rows[0]["StartDate"], result[0].ReservationStart);
        Assert.Equal(datatable.Rows[0]["EndDate"], result[0].ReservationEnd);
    }

    [Fact]
    public void GetAllReservations_ShouldReturnEmptyList_WhenNoReservations()
    {
        var datatable = new DataTable();
        _mockDataGateway.Setup(m => m.GetAllReservations()).Returns(datatable);
        
        var result = _reservationService.GetAllReservations();
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetReservationsByUserId Tests

    [Fact]
    public void GetReservationsByUserId_ShouldReturnReservationsForUser()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ReservationID", typeof(int));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("UserID", typeof(int));   
        datatable.Columns.Add("StartDate", typeof(DateTime));
        datatable.Columns.Add("EndDate", typeof(DateTime));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Rows.Add(1, 1, 1, DateTime.Now, DateTime.Now.AddHours(1), "1");
        _mockDataGateway.Setup(m => m.GetReservationsByUserId(1)).Returns(datatable);
        var result = _reservationService.GetReservationsByUserId(1);
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].ServerId);
        Assert.Equal(1, result[0].UserId);
        Assert.Equal("1", result[0].LabId);
        Assert.Equal(datatable.Rows[0]["StartDate"], result[0].ReservationStart);
        Assert.Equal(datatable.Rows[0]["EndDate"], result[0].ReservationEnd);
    }

    [Fact]
    public void GetReservationsByUserId_ShouldReturnEmptyList_WhenNoReservationsForUser()
    {
        var datatable = new DataTable();
        _mockDataGateway.Setup(m => m.GetReservationsByUserId(1)).Returns(datatable);
        
        var result = _reservationService.GetReservationsByUserId(1);
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }


    [Fact]
    public void GetReservationsByUserId_ShouldReturnNull_WhenDataGatewayReturnsNull()
    {
        _mockDataGateway.Setup(m => m.GetReservationsByUserId(1)).Returns((DataTable)null);
        
        var result = _reservationService.GetReservationsByUserId(1);
        
        Assert.Null(result);
    }

    #endregion

    #region GetReservationsByServerId Tests

    [Fact]
    public void GetReservationsByServerId_ShouldReturnReservationsForServer()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ReservationID", typeof(int));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("UserID", typeof(int));   
        datatable.Columns.Add("StartDate", typeof(DateTime));
        datatable.Columns.Add("EndDate", typeof(DateTime));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Rows.Add(1, 1, 1, DateTime.Now, DateTime.Now.AddHours(1), "1");
        _mockDataGateway.Setup(m => m.GetReservationsByServerId(1)).Returns(datatable);
        
        var result = _reservationService.GetReservationsByServerId(1);
        
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].ServerId);
        Assert.Equal(1, result[0].UserId);
        Assert.Equal("1", result[0].LabId);
        Assert.Equal(datatable.Rows[0]["StartDate"], result[0].ReservationStart);
        Assert.Equal(datatable.Rows[0]["EndDate"], result[0].ReservationEnd);
    }

    [Fact]
    public void GetReservationsByServerId_ShouldReturnEmptyList_WhenNoReservationsForServer()
    {
        var datatable = new DataTable();
        _mockDataGateway.Setup(m => m.GetReservationsByServerId(1)).Returns(datatable);
        
        var result = _reservationService.GetReservationsByServerId(1);
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }


    [Fact]
    public void GetReservationsByServerId_ShouldReturnNull_WhenDataGatewayReturnsNull()
    {
        _mockDataGateway.Setup(m => m.GetReservationsByServerId(1)).Returns((DataTable)null);
        
        var result = _reservationService.GetReservationsByServerId(1);
        
        Assert.Null(result);
    }

    #endregion

    #region MakeReservation Tests

    [Fact]
    public void MakeReservation_ShouldReturnTrue_WhenReservationIsSuccessful()
    {
        var reservation = new ReservationModel
        {
            ServerId = 1,
            UserId = 1,
            LabId = "1",
            ReservationStart = DateTime.Now,
            ReservationEnd = DateTime.Now.AddHours(1)
        };
        _mockDataGateway.Setup(m => m.GetAllReservations()).Returns(new DataTable());
        _mockDataGateway.Setup(m => m.InsertReservation(reservation.ServerId, reservation.UserId, reservation.ReservationStart, reservation.ReservationEnd, reservation.LabId));
        
        var result = _reservationService.MakeReservation(reservation);
        
        Assert.True(result);
        _mockDataGateway.Verify(m => m.InsertReservation(reservation.ServerId, reservation.UserId, reservation.ReservationStart, reservation.ReservationEnd, reservation.LabId), Times.Once);
    }

    [Fact]
    public void MakeReservation_ShouldReturnFalse_WhenReservationOverlaps()
    {
        var reservation = new ReservationModel
        {
            ServerId = 1,
            UserId = 1,
            LabId = "1",
            ReservationStart = DateTime.Now,
            ReservationEnd = DateTime.Now.AddHours(1)
        };
        var datatable = new DataTable();
        datatable.Columns.Add("ReservationID", typeof(int));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("UserID", typeof(int));   
        datatable.Columns.Add("StartDate", typeof(DateTime));
        datatable.Columns.Add("EndDate", typeof(DateTime));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Rows.Add(1, 1, 1, DateTime.Now, DateTime.Now.AddHours(1), "1");
        _mockDataGateway.Setup(m => m.GetAllReservations()).Returns(datatable);
        
        var result = _reservationService.MakeReservation(reservation);
        
        Assert.False(result);
        _mockDataGateway.Verify(m => m.InsertReservation(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }


    #endregion


    #region DeleteReservation Tests

    [Fact]
    public void DeleteReservation_ShouldReturnTrue_WhenDeletionIsSuccessful()
    {
        _mockDataGateway.Setup(m => m.RemoveReservation(1));
        
        var result = _reservationService.DeleteReservation(1);
        
        Assert.True(result);
        _mockDataGateway.Verify(m => m.RemoveReservation(1), Times.Once);
    }

    [Fact]
    public void DeleteReservation_ShouldReturnFalse_WhenDeletionFails()
    {
        _mockDataGateway.Setup(m => m.RemoveReservation(1)).Throws(new Exception());
        
        var result = _reservationService.DeleteReservation(1);
        
        Assert.False(result);
        _mockDataGateway.Verify(m => m.RemoveReservation(1), Times.Once);
    }

    #endregion

    #region SaveReservation Tests
    
    [Fact]
    public void SaveReservation_ShouldReturnStringBuilderICS_WhenSaveIsSuccessful()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ReservationID", typeof(int));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("UserID", typeof(int));   
        datatable.Columns.Add("StartDate", typeof(DateTime));
        datatable.Columns.Add("EndDate", typeof(DateTime));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Rows.Add(1, 1, 1, DateTime.Now, DateTime.Now.AddHours(1), "1");
        _mockDataGateway.Setup(m => m.GetReservationById(1)).Returns(datatable);
        
        var result = _reservationService.SaveReservation(1);
        
        Assert.NotNull(result);
        Assert.Contains("BEGIN:VCALENDAR", result.ToString());
        Assert.Contains("VERSION:2.0", result.ToString());
        Assert.Contains("BEGIN:VEVENT", result.ToString());
        Assert.Contains("SUMMARY:Reservation of lab", result.ToString());
        Assert.Contains("LOCATION:", result.ToString());
        Assert.Contains("DESCRIPTION:", result.ToString());
        Assert.Contains("END:VEVENT", result.ToString());
        Assert.Contains("END:VCALENDAR", result.ToString());
    }

    #endregion
}