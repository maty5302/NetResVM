using System.Data;
using DataLayer;
using Xunit;
using System;

namespace NetResVM.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("Integration DB")]
public class UserLabOwnershipTableDataGatewayTests
{
    private readonly UserLabOwnershipTableDataGateway _gateway;
    private readonly UserTableDataGateway _userGateway;
    private readonly ServerTableDataGateway _serverGateway;

    public UserLabOwnershipTableDataGatewayTests(DatabaseFixture fixture)
    {
        DBConnector.TestConnectionString = fixture.ConnectionString;
        _gateway = new UserLabOwnershipTableDataGateway();
        _userGateway = new UserTableDataGateway();
        _serverGateway = new ServerTableDataGateway();
    }

    [Fact]
    public void InsertUserLabOwnership_WithValidReferences_ShouldPersistAndBeQueryable()
    {
        // Arrange
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("insert-valid");

        // Act
        _gateway.InsertUserLabOwnership(userId, labId, serverId);

        // Assert
        DataTable byUser = _gateway.GetAllUserLabsByUserId(userId);
        DataTable byLab = _gateway.GetAllUserLabsByLabId(labId);

        Assert.True(TableContainsOwnership(byUser, userId, labId, serverId));
        Assert.True(TableContainsOwnership(byLab, userId, labId, serverId));
    }

    [Fact]
    public void GetAllUserLabsByUserId_WhenUserHasNoOwnership_ShouldReturnEmptyTable()
    {
        // Arrange
        int userId = CreateUserForTest();

        // Act
        DataTable result = _gateway.GetAllUserLabsByUserId(userId);

        // Assert
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void GetAllUserLabsByLabId_WhenLabHasNoOwnership_ShouldReturnEmptyTable()
    {
        // Arrange
        string labId = CreateUniqueLabId("missing-lab");

        // Act
        DataTable result = _gateway.GetAllUserLabsByLabId(labId);

        // Assert
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void DeleteUserLabOwnership_WhenOwnershipExists_ShouldRemoveOnlyTargetRow()
    {
        // Arrange
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labIdToDelete = CreateUniqueLabId("delete-target");
        string labIdToKeep = CreateUniqueLabId("delete-keep");

        _gateway.InsertUserLabOwnership(userId, labIdToDelete, serverId);
        _gateway.InsertUserLabOwnership(userId, labIdToKeep, serverId);

        // Act
        _gateway.DeleteUserLabOwnership(userId, labIdToDelete, serverId);

        // Assert
        DataTable byUser = _gateway.GetAllUserLabsByUserId(userId);
        Assert.False(TableContainsOwnership(byUser, userId, labIdToDelete, serverId));
        Assert.True(TableContainsOwnership(byUser, userId, labIdToKeep, serverId));
    }

    [Fact]
    public void DeleteUserLabOwnership_WhenOwnershipDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string missingLabId = CreateUniqueLabId("missing-delete");

        // Act & Assert
        _gateway.DeleteUserLabOwnership(userId, missingLabId, serverId);
    }

    [Fact]
    public void InsertUserLabOwnership_WithDuplicateCompositeKey_ShouldThrow()
    {
        // Arrange
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("duplicate");
        _gateway.InsertUserLabOwnership(userId, labId, serverId);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _gateway.InsertUserLabOwnership(userId, labId, serverId));
    }

    [Fact]
    public void InsertUserLabOwnership_WithMissingUserReference_ShouldThrow()
    {
        // Arrange
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("missing-user");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _gateway.InsertUserLabOwnership(int.MaxValue, labId, serverId));
    }

    [Fact]
    public void InsertUserLabOwnership_WithMissingServerReference_ShouldThrow()
    {
        // Arrange
        int userId = CreateUserForTest();
        string labId = CreateUniqueLabId("missing-server");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _gateway.InsertUserLabOwnership(userId, labId, int.MaxValue));
    }

    private int CreateUserForTest()
    {
        string username = CreateUniqueValue("owner");
        _userGateway.AddUser(username, "password", "student", "localhost", 1);

        DataTable result = _userGateway.GetUserByUsername(username);
        Assert.Single(result.Rows);
        return Convert.ToInt32(result.Rows[0]["UserID"]);
    }

    private int CreateServerForTest()
    {
        string name = CreateUniqueValue("server");
        _serverGateway.InsertServer("CML", name, "192.168.10.10", "admin", "password");

        DataTable result = _serverGateway.GetAllServers();
        DataRow[] rows = result.Select($"Name = '{name.Replace("'", "''")}'");
        Assert.Single(rows);
        return Convert.ToInt32(rows[0]["ServerID"]);
    }

    private static string CreateUniqueLabId(string prefix)
    {
        return CreateUniqueValue(prefix);
    }

    private static string CreateUniqueValue(string prefix)
    {
        string value = $"{prefix}-{Guid.NewGuid():N}";
        return value.Substring(0, Math.Min(value.Length, 50));
    }

    private static bool TableContainsOwnership(DataTable table, int userId, string labId, int serverId)
    {
        foreach (DataRow row in table.Rows)
        {
            if (Convert.ToInt32(row["UserID"]) == userId
                && string.Equals(Convert.ToString(row["LabID"]), labId, StringComparison.Ordinal)
                && Convert.ToInt32(row["ServerID"]) == serverId)
            {
                return true;
            }
        }

        return false;
    }
}