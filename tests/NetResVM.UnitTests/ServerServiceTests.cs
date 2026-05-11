using BusinessLayer.Services;
using DataLayer.Interface;
using System.Data;
using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Models;
using Moq;

namespace NetResVM.UnitTests;

public class ServerServiceTests
{
    private readonly Mock<IServerTableDataGateway> _mockDataGateway;
    private readonly ServerService _serverService;

    public ServerServiceTests()
    {
        _mockDataGateway = new Mock<IServerTableDataGateway>();
        _serverService = new ServerService(_mockDataGateway.Object);
    }

    public void Dispose()
    {
        //Cleanup if needed
    }

    #region GetAllServers Tests

    [Fact]
    public void GetAllServers_WithValidData_ReturnsListOfServers()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        datatable.Rows.Add(2, "EVE", "ServerD", "10.0.0.4", "admin", "eve");

        _mockDataGateway.Setup(x => x.GetAllServers()).Returns(datatable);

        var result = _serverService.GetAllServers();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count == 2);
    }


    [Fact]
    public void GetAllServers_WithNoData_ReturnsEmptyList()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));

        _mockDataGateway.Setup(x => x.GetAllServers()).Returns(datatable);

        var result = _serverService.GetAllServers();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetServerById

    [Fact]
    public void GetServerById_WithValidData_ReturnServer()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        datatable.Rows.Add(2, "EVE", "ServerD", "10.0.0.4", "admin", "eve");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.GetServerById(1);
        Assert.NotNull(result);
        Assert.IsType<ServerDTO>(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(PlatformType.CML, result.Platform);
        Assert.Equal("ServerA", result.Name);
        Assert.Equal("10.0.0.1", result.IpAddress);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public void GetServerById_WithNoData_ReturnsNull()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.GetServerById(1);
        Assert.Null(result);
    }

    #endregion

    #region GetServerCredentials Tests

    [Fact]
    public void GetServerCredentials_WithValidData_ReturnServerCredentials()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        datatable.Rows.Add(2, "EVE", "ServerD", "10.0.0.4", "admin", "eve");
        
        _mockDataGateway.Setup(x=>x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.GetServerCredentials(1); 
        Assert.NotNull(result);
        Assert.Equal("10.0.0.1",result.Value.Url);
        Assert.Equal("admin",result.Value.Username);
        Assert.Equal("password",result.Value.Password);
    }

    [Fact]
    public void GetServerCredentials_WithNoData_ReturnsNull()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        var result = _serverService.GetServerCredentials(1);
        Assert.Null(result);
    }
    #endregion

    #region GetServerType Tests

    [Fact]
    public void GetServerType_WithValidData_ReturnServerType()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        datatable.Rows.Add(2, "EVE", "ServerD", "10.0.0.4", "admin", "eve");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.GetServerType(1);
        Assert.IsType<PlatformType>(result);
        Assert.Equal(PlatformType.CML, result); 
    }

    [Fact]
    public void GetServerType_WithNoData_ReturnsPlatformTypeUnknown()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.GetServerType(1);
        Assert.IsType<PlatformType>(result);  
        Assert.Equal(PlatformType.Unknown, result);
    }

    #endregion

    #region ServerExists Tests

    [Fact]
    public void ServerExists_WithValidData_ReturnTrue()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        datatable.Rows.Add(2, "EVE", "ServerD", "10.0.0.4", "admin", "eve");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        var result = _serverService.ServerExists(1);
        Assert.True(result);
    }

    [Fact]
    public void ServerExists_WithNoData_ReturnsFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        var result = _serverService.ServerExists(1);
        Assert.False(result);
    }
    
    #endregion

    #region InsertServer Tests

    [Fact]
    public void InsertServer_WithValidData_ReturnsTrue()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetAllServers()).Returns(datatable);

        ServerModel serverModel = new ServerModel()
        {
            Id = 4,
            Name = "ServerB",
            Platform = PlatformType.EVE,
            IpAddress = "10.0.0.4",
            Username = "admin",
            Password = "password",
        };
        
        var result =  _serverService.InsertServer(serverModel);
        Assert.True(result);
    }

    [Fact]
    public void InsertServer_WithExistingServer_ReturnsFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetAllServers()).Returns(datatable);
        
        ServerModel serverModel = new ServerModel()
        {
            Id = 1,
            Name = "ServerA",
            Platform = PlatformType.CML,
            IpAddress = "10.0.0.1",
            Username = "admin",
            Password = "password",
        };
        
        var result =   _serverService.InsertServer(serverModel);
        Assert.False(result);
    }

    #endregion

    #region UpdateServer Tests

    [Fact]
    public void UpdateServer_WithValidData_ReturnsTrue()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);

        ServerModel ser = new ServerModel()
        {
            Id = 1,
            Name = "ServerA",
            Platform = PlatformType.CML,
            IpAddress = "10.0.0.4",
            Username = "admin",
            Password = "password",
        };

        var result = _serverService.UpdateServer(ser);
        Assert.True(result);
    }

    [Fact]
    public void UpdateServer_UnknownServerId_ReturnsFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);

        ServerModel ser = new ServerModel()
        {
            Id = 4,
            Name = "ServerA",
            Platform = PlatformType.CML,
            IpAddress = "10.0.0.4",
            Username = "admin",
            Password = "password",
        };

        var result = _serverService.UpdateServer(ser);
        Assert.False(result);
    }

    #endregion
    
    #region RemoveServer Tests

    [Fact]
    public void RemoveServer_WithValidData_ReturnsTrue()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.RemoveServer(1);
        Assert.True(result);
        
        _mockDataGateway.Verify(x => x.RemoveServer(1), Times.Once);
    }

    [Fact]
    public void RemoveServer_UnknownServerId_ReturnsFalse()
    {
        var datatable = new DataTable();
        datatable.Columns.Add("ServerID", typeof(int));
        datatable.Columns.Add("ServerType", typeof(string));
        datatable.Columns.Add("Name", typeof(string));
        datatable.Columns.Add("IpAddress", typeof(string));
        datatable.Columns.Add("Username", typeof(string));
        datatable.Columns.Add("Password", typeof(string));
        datatable.Rows.Add(1, "CML", "ServerA", "10.0.0.1", "admin", "password");
        
        _mockDataGateway.Setup(x => x.GetServerById(1)).Returns(datatable);
        
        var result = _serverService.RemoveServer(4);
        Assert.False(result);
    }
    
    #endregion
}