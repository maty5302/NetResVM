using System.Data;
using System.Reflection;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Interface;
using Moq;
using NuGet.Frameworks;
using Xunit;

namespace NetResVM.UnitTests;

public class UserLabOwnershipServiceTests
{
    private readonly UserLabOwnershipService _labOwnershipService;
    private readonly Mock<IUserLabOwnershipTableDataGateway> _mockDataGateway;
    private readonly Mock<IUserTableDataGateway> _mockUserGateway;
    
    public UserLabOwnershipServiceTests()
    {
        _mockDataGateway = new Mock<IUserLabOwnershipTableDataGateway>();
        _mockUserGateway = new Mock<IUserTableDataGateway>();
        _labOwnershipService = new UserLabOwnershipService(_mockDataGateway.Object, _mockUserGateway.Object);
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }

    #region GetAllUserLabsByUserID Tests

    [Fact]
    public void GetUserLabsByUserId_WithValidUser_ShouldReturnAllUserLabs()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        datatable.Rows.Add(1, "LabB", 102);
        datatable.Rows.Add(1, "LabC", 103);
        
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(1)).Returns(datatable);
        
        var result = _labOwnershipService.GetAllUserLabsByUserID(1);
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.IsType<List<UserLabOwnershipModel>>(result);
        Assert.Contains(result, r => r.LabId == "LabA" && r.ServerId == 101 && r.UserId == 1);
        Assert.Contains(result, r => r.LabId == "LabB" && r.ServerId == 102 && r.UserId == 1);
        Assert.Contains(result, r => r.LabId == "LabC" && r.ServerId == 103 && r.UserId == 1);
    }

    [Fact]
    public void GetUserLabsByUserId_WithInvalidUser_ShouldReturnEmptyList()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(487)).Returns(datatable);  
        
        var result = _labOwnershipService.GetAllUserLabsByUserID(487);
        Assert.NotNull(result);
        Assert.IsType<List<UserLabOwnershipModel>>(result);
        Assert.Empty(result);   
    }

    #endregion

    #region GetAllUserLabsByLabId Tests

    [Fact]
    public void GetUserLabsByLabId_WithValidLab_ShouldReturnAllUserLabs()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        datatable.Rows.Add(2, "LabA", 101);

        _mockDataGateway.Setup(d => d.GetAllUserLabsByLabId("LabA")).Returns(datatable);

        var result = _labOwnershipService.GetAllUserLabsByLabId("LabA");
        Assert.NotNull(result);
        Assert.IsType<List<UserLabOwnershipModel>>(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.LabId == "LabA" && r.ServerId == 101 && r.UserId == 1);
        Assert.Contains(result, r => r.LabId == "LabA" && r.ServerId == 101 && r.UserId == 2);
    }

    [Fact]
    public void GetUserLabsByLabId_WithInvalidLab_ShouldReturnEmptyList()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        _mockDataGateway.Setup(d => d.GetAllUserLabsByLabId("NonExistentLab")).Returns(datatable);

        var result = _labOwnershipService.GetAllUserLabsByLabId("NonExistentLab");
        Assert.NotNull(result);
        Assert.IsType<List<UserLabOwnershipModel>>(result);
        Assert.Empty(result);
    }

    #endregion

    #region IsLabAlreadyOwned Tests

    [Fact]
    public void IsLabAlreadyOwned_WithUser_LabHeOwns_ShouldReturnTrueTrue()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        
        _mockDataGateway.Setup(d => d.GetAllUserLabsByLabId("LabA")).Returns(datatable);
        
        var result = _labOwnershipService.IsLabAlreadyOwned(1, "LabA");
        Assert.True(result.owned);
        Assert.True(result.userOwns);
    }

    [Fact]
    public void IsLabAlreadyOwned_WithUser_LabHeDoesntOwn_ShouldReturnTrueFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        
        _mockDataGateway.Setup(d => d.GetAllUserLabsByLabId("LabA")).Returns(datatable);
        
        var result = _labOwnershipService.IsLabAlreadyOwned(2, "LabA");
        Assert.True(result.owned); 
        Assert.False(result.userOwns);
    }

    [Fact]
    public void IsLabAlreadyOwned_WithUser_LabIsntOwned_ShouldReturnFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        
        _mockDataGateway.Setup(d => d.GetAllUserLabsByLabId("LabA")).Returns(datatable);
        var result = _labOwnershipService.IsLabAlreadyOwned(1, "LabA");
        Assert.False(result.owned); 
        Assert.False(result.userOwns);
    }
    #endregion

    #region InsertUserLabOwnership Tests

    [Fact]
    public void InsertUserLabOwnership_WithValidUser_ShouldInsertUserLabOwnership()
    {
        var validUserTable = new DataTable();
        validUserTable.Columns.Add("Id", typeof(int));
        validUserTable.Rows.Add(1);
        _mockUserGateway.Setup(u => u.GetUserById(1)).Returns(validUserTable);
        
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        _mockDataGateway.Setup(d => d.InsertUserLabOwnership(1, "LabA", 101)).Callback(() =>
        {
            datatable.Rows.Add(1, "LabA", 101);
        });
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(1)).Returns(datatable);
        
        var result = _labOwnershipService.InsertUserLabOwnership(new UserLabOwnershipModel
        {
            UserId = 1,
            LabId = "LabA",
            ServerId = 101
        });
        Assert.True(result.Item1);
        
    }
    
    [Fact]
    public void InsertUserLabOwnership_WithNonExistentUser_ShouldReturnFalse()
    {
        var emptyUserTable = new DataTable();
        emptyUserTable.Columns.Add("Id", typeof(int));
        _mockUserGateway.Setup(u => u.GetUserById(999)).Returns(emptyUserTable);

        var result = _labOwnershipService.InsertUserLabOwnership(new UserLabOwnershipModel
        {
            UserId = 999, 
            LabId = "LabA",
            ServerId = 101
        });

        Assert.False(result.Item1);
        Assert.Equal("User does not exist.", result.Item2);
        
        _mockDataGateway.Verify(d => d.InsertUserLabOwnership(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
    
    [Fact]
    public void InsertUserLabOwnership_WithExistingLab_ShouldReturnFalse()
    {
        var validUserTable = new DataTable();
        validUserTable.Columns.Add("Id", typeof(int));
        validUserTable.Rows.Add(1);
        _mockUserGateway.Setup(u => u.GetUserById(1)).Returns(validUserTable);
        
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(1)).Returns(datatable);
        
        var result = _labOwnershipService.InsertUserLabOwnership(new UserLabOwnershipModel
        {
            UserId = 1,
            LabId = "LabA",
            ServerId = 101
        });
        
        Assert.False(result.Item1);
        Assert.Equal("You already own this lab.", result.Item2);
        
        _mockDataGateway.Verify(d => d.InsertUserLabOwnership(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
    
    #endregion

    #region DeleteUserLabOwnership Tests

    [Fact]
    public void DeleteUserLabOwnership_WithValidUser_ShouldDeleteUserLabOwnership()
    {
        var validUserTable = new DataTable();
        validUserTable.Columns.Add("Id", typeof(int));
        validUserTable.Rows.Add(1);
        _mockUserGateway.Setup(u => u.GetUserById(1)).Returns(validUserTable);
        
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Rows.Add(1, "LabA", 101);
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(1)).Returns(datatable);
        
        var result = _labOwnershipService.DeleteUserLabOwnership(new UserLabOwnershipModel()
        {
            UserId = 1,
            LabId = "LabA",
            ServerId = 101
        });
        
        Assert.True(result);
        _mockDataGateway.Verify(d => d.DeleteUserLabOwnership(1, "LabA", 101), Times.Once);
    }

    [Fact]
    public void DeleteUserLabOwnership_WithNonExistentUser_ShouldReturnFalse()
    {
        var validUserTable = new DataTable();
        validUserTable.Columns.Add("Id", typeof(int));
        validUserTable.Rows.Add(1);
        _mockUserGateway.Setup(u => u.GetUserById(1)).Returns(validUserTable);
        
        var datatable = new DataTable();
        datatable.Columns.Add("UserID", typeof(int));
        datatable.Columns.Add("LabID", typeof(string));
        datatable.Columns.Add("ServerID", typeof(int));
        
        datatable.Rows.Add(1, "LabA", 101);
        _mockDataGateway.Setup(d => d.GetAllUserLabsByUserId(1)).Returns(datatable);
        
        var result = _labOwnershipService.DeleteUserLabOwnership(new UserLabOwnershipModel()
        {
            UserId = 999, 
            LabId = "LabA",
            ServerId = 101
        });
        
        Assert.False(result);
        _mockDataGateway.Verify(d => d.DeleteUserLabOwnership(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }
    #endregion
}