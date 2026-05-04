using System.Data;
using DataLayer;

namespace NetResVM.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("Integration DB")]
public class ServerTableDataGatewayTests
{
    private readonly ServerTableDataGateway _gateway;

    public ServerTableDataGatewayTests(DatabaseFixture fixture)
    {
        DBConnector.TestConnectionString = fixture.ConnectionString;
        _gateway = new ServerTableDataGateway();
    }

    #region InsertServer Tests

    [Fact]
    public void InsertServer_WithValidData_ShouldPersistAllFields()
    {
        // Arrange
        string serverType = "CML";
        string name = CreateUniqueServerName("cml-server");
        string ipAddress = "192.168.1.100";
        string username = "admin";
        string password = "SecurePass123!";

        // Act
        _gateway.InsertServer(serverType, name, ipAddress, username, password);

        // Assert
        DataRow row = GetSingleServerRowByName(name);
        Assert.Equal(serverType, row["ServerType"]);
        Assert.Equal(name, row["Name"]);
        Assert.Equal(ipAddress, row["IpAddress"]);
        Assert.Equal(username, row["Username"]);
        Assert.Equal(password, row["Password"]);
    }

    [Fact]
    public void InsertServer_WithEveType_ShouldPersistCorrectly()
    {
        // Arrange
        string serverType = "EVE";
        string name = CreateUniqueServerName("eve-server");
        string ipAddress = "10.0.0.50";
        string username = "testuser";
        string password = "TestPass456!";

        // Act
        _gateway.InsertServer(serverType, name, ipAddress, username, password);

        // Assert
        DataRow row = GetSingleServerRowByName(name);
        Assert.Equal("EVE", row["ServerType"]);
        Assert.Equal(name, row["Name"]);
    }

    #endregion

    #region GetServerById Tests

    [Fact]
    public void GetServerById_WhenServerExists_ShouldReturnServer()
    {
        // Arrange
        string name = CreateUniqueServerName("get-by-id");
        _gateway.InsertServer("CML", name, "192.168.1.200", "admin", "password");
        int serverId = GetSingleServerIdByName(name);

        // Act
        DataTable result = _gateway.GetServerById(serverId);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(serverId, result.Rows[0]["ServerID"]);
        Assert.Equal(name, result.Rows[0]["Name"]);
    }

    [Fact]
    public void GetServerById_WhenServerDoesNotExist_ShouldReturnEmptyTable()
    {
        // Act
        DataTable result = _gateway.GetServerById(int.MaxValue);

        // Assert
        Assert.Empty(result.Rows);
    }

    #endregion

    #region GetAllServers Tests

    [Fact]
    public void GetAllServers_ShouldContainInsertedServer()
    {
        // Arrange
        string name = CreateUniqueServerName("get-all");
        int rowsBefore = _gateway.GetAllServers().Rows.Count;

        // Act
        _gateway.InsertServer("CML", name, "172.16.0.1", "root", "admin123");
        DataTable result = _gateway.GetAllServers();

        // Assert
        Assert.Equal(rowsBefore + 1, result.Rows.Count);
        Assert.True(TableContainsServerName(result, name));
    }

    [Fact]
    public void GetAllServers_WithMultipleServers_ShouldReturnAll()
    {
        // Arrange
        string name1 = CreateUniqueServerName("multi-1");
        string name2 = CreateUniqueServerName("multi-2");

        _gateway.InsertServer("CML", name1, "192.168.1.1", "user1", "pass1");
        _gateway.InsertServer("EVE", name2, "192.168.1.2", "user2", "pass2");

        // Act
        DataTable result = _gateway.GetAllServers();

        // Assert
        Assert.True(TableContainsServerName(result, name1));
        Assert.True(TableContainsServerName(result, name2));
    }

    #endregion

    #region UpdateServer Tests

    [Fact]
    public void UpdateServer_ShouldPersistAllUpdatedFields()
    {
        // Arrange
        string name = CreateUniqueServerName("update-test");
        _gateway.InsertServer("CML", name, "192.168.1.100", "olduser", "oldpass");
        int serverId = GetSingleServerIdByName(name);

        // Act
        _gateway.UpdateServer(serverId, "EVE", "updated-name", "10.0.0.1", "newuser", "newpass");

        // Assert
        DataRow row = GetSingleServerRowById(serverId);
        Assert.Equal("EVE", row["ServerType"]);
        Assert.Equal("updated-name", row["Name"]);
        Assert.Equal("10.0.0.1", row["IpAddress"]);
        Assert.Equal("newuser", row["Username"]);
        Assert.Equal("newpass", row["Password"]);
    }

    [Fact]
    public void UpdateServer_WithPartialChanges_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        string name = CreateUniqueServerName("partial-update");
        _gateway.InsertServer("CML", name, "192.168.1.50", "admin", "password123");
        int serverId = GetSingleServerIdByName(name);

        // Act
        _gateway.UpdateServer(serverId, "CML", name, "192.168.2.50", "admin", "newpassword456");

        // Assert
        DataRow row = GetSingleServerRowById(serverId);
        Assert.Equal("192.168.2.50", row["IpAddress"]);
        Assert.Equal("newpassword456", row["Password"]);
        Assert.Equal(name, row["Name"]); // Unchanged
    }

    #endregion

    #region RemoveServer Tests

    [Fact]
    public void RemoveServer_ShouldDeleteServerFromDatabase()
    {
        // Arrange
        string name = CreateUniqueServerName("remove-test");
        _gateway.InsertServer("CML", name, "192.168.1.150", "admin", "password");
        int serverId = GetSingleServerIdByName(name);

        // Act
        _gateway.RemoveServer(serverId);

        // Assert
        DataTable result = _gateway.GetServerById(serverId);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void RemoveServer_NonExistentServer_ShouldNotThrow()
    {
        // Act & Assert (should not throw exception)
        _gateway.RemoveServer(int.MaxValue);
    }

    #endregion

    #region Helper Methods

    private static string CreateUniqueServerName(string prefix)
    {
        string value = $"{prefix}-{Guid.NewGuid():N}";
        return value.Substring(0, Math.Min(value.Length, 100));
    }

    private DataRow GetSingleServerRowByName(string name)
    {
        DataTable result = _gateway.GetAllServers();
        DataRow[] rows = result.Select($"Name = '{name.Replace("'", "''")}'");

        Assert.Single(rows);
        return rows[0];
    }

    private DataRow GetSingleServerRowById(int id)
    {
        DataTable result = _gateway.GetServerById(id);

        Assert.Single(result.Rows);
        return result.Rows[0];
    }

    private int GetSingleServerIdByName(string name)
    {
        DataRow row = GetSingleServerRowByName(name);
        return Convert.ToInt32(row["ServerID"]);
    }

    private static bool TableContainsServerName(DataTable table, string name)
    {
        foreach (DataRow row in table.Rows)
        {
            if (string.Equals(Convert.ToString(row["Name"]), name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}