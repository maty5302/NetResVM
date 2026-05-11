using System.Data;
using DataLayer;

namespace NetResVM.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("Integration DB")]
public class ReservationTableDataGatewayTests
{
    private readonly ReservationTableDataGateway _gateway;
    private readonly UserTableDataGateway _userGateway;
    private readonly ServerTableDataGateway _serverGateway;

    public ReservationTableDataGatewayTests(DatabaseFixture fixture)
    {
        DBConnector.TestConnectionString = fixture.ConnectionString;
        _gateway = new ReservationTableDataGateway();
        _userGateway = new UserTableDataGateway();
        _serverGateway = new ServerTableDataGateway();
    }

    [Fact]
    public void InsertReservation_WithValidData_ShouldPersistAllFields()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-valid");
        DateTime startTime = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(2);

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);

        DataRow row = GetSingleReservationRowByLabId(labId);
        Assert.Equal(userId, row["UserID"]);
        Assert.Equal(serverId, row["ServerID"]);
        Assert.Equal(labId, row["LabID"]);
        Assert.Equal(startTime, Convert.ToDateTime(row["StartDate"]));
        Assert.Equal(endTime, Convert.ToDateTime(row["EndDate"]));
    }

    [Fact]
    public void GetReservationById_WhenReservationExists_ShouldReturnReservation()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-by-id");
        DateTime startTime = new DateTime(2030, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(1);

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);
        int reservationId = GetSingleReservationIdByLabId(labId);

        DataTable result = _gateway.GetReservationById(reservationId);

        Assert.Single(result.Rows);
        Assert.Equal(reservationId, result.Rows[0]["ReservationID"]);
        Assert.Equal(userId, result.Rows[0]["UserID"]);
        Assert.Equal(serverId, result.Rows[0]["ServerID"]);
    }

    [Fact]
    public void GetReservationById_WhenReservationDoesNotExist_ShouldReturnEmptyTable()
    {
        DataTable result = _gateway.GetReservationById(int.MaxValue);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void GetReservationsByUserId_WhenUserHasReservation_ShouldReturnReservation()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-by-user");
        DateTime startTime = new DateTime(2030, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(3);

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);

        DataTable result = _gateway.GetReservationsByUserId(userId);

        Assert.Single(result.Rows);
        Assert.True(TableContainsReservation(result, userId, labId, serverId, startTime, endTime));
    }

    [Fact]
    public void GetReservationsByUserId_WhenUserHasNoReservations_ShouldReturnEmptyTable()
    {
        int userId = CreateUserForTest();

        DataTable result = _gateway.GetReservationsByUserId(userId);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void GetReservationsByServerId_WhenServerHasReservation_ShouldReturnReservation()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-by-server");
        DateTime startTime = new DateTime(2030, 4, 5, 6, 7, 8, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(4);

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);

        DataTable result = _gateway.GetReservationsByServerId(serverId);

        Assert.Single(result.Rows);
        Assert.True(TableContainsReservation(result, userId, labId, serverId, startTime, endTime));
    }

    [Fact]
    public void GetReservationsByServerId_WhenServerHasNoReservations_ShouldReturnEmptyTable()
    {
        int serverId = CreateServerForTest();

        DataTable result = _gateway.GetReservationsByServerId(serverId);

        Assert.Empty(result.Rows);
    }

    [Fact]
    public void GetAllReservations_ShouldContainInsertedReservation()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-all");
        DateTime startTime = new DateTime(2030, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(5);

        int rowsBefore = _gateway.GetAllReservations().Rows.Count;

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);

        DataTable result = _gateway.GetAllReservations();

        Assert.Equal(rowsBefore + 1, result.Rows.Count);
        Assert.True(TableContainsReservation(result, userId, labId, serverId, startTime, endTime));
    }

    [Fact]
    public void RemoveReservation_ShouldDeleteReservationFromDatabase()
    {
        int userId = CreateUserForTest();
        int serverId = CreateServerForTest();
        string labId = CreateUniqueLabId("reservation-remove");
        DateTime startTime = new DateTime(2030, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(6);

        _gateway.InsertReservation(serverId, userId, startTime, endTime, labId);
        int reservationId = GetSingleReservationIdByLabId(labId);

        _gateway.RemoveReservation(reservationId);

        Assert.Empty(_gateway.GetReservationById(reservationId).Rows);
    }

    [Fact]
    public void RemoveReservation_WhenReservationDoesNotExist_ShouldNotThrow()
    {
        _gateway.RemoveReservation(int.MaxValue);
    }

    [Fact]
    public void InsertReservation_WithMissingUserReference_ShouldThrow()
    {
        int serverId = CreateServerForTest();
        DateTime startTime = new DateTime(2030, 7, 8, 9, 10, 11, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(1);

        Assert.ThrowsAny<Exception>(() => _gateway.InsertReservation(serverId, int.MaxValue, startTime, endTime, CreateUniqueLabId("missing-user")));
    }

    [Fact]
    public void InsertReservation_WithMissingServerReference_ShouldThrow()
    {
        int userId = CreateUserForTest();
        DateTime startTime = new DateTime(2030, 8, 9, 10, 11, 12, DateTimeKind.Utc);
        DateTime endTime = startTime.AddHours(1);

        Assert.ThrowsAny<Exception>(() => _gateway.InsertReservation(int.MaxValue, userId, startTime, endTime, CreateUniqueLabId("missing-server")));
    }

    private int CreateUserForTest()
    {
        string username = CreateUniqueValue("reservation-user");
        _userGateway.AddUser(username, "password", "student", "localhost", 1);

        DataTable result = _userGateway.GetUserByUsername(username);
        Assert.Single(result.Rows);
        return Convert.ToInt32(result.Rows[0]["UserID"]);
    }

    private int CreateServerForTest()
    {
        string name = CreateUniqueValue("reservation-server");
        _serverGateway.InsertServer("CML", name, "192.168.10.10", "admin", "password");

        DataTable result = _serverGateway.GetAllServers();
        DataRow[] rows = result.Select($"Name = '{name.Replace("'", "''")}'");
        Assert.Single(rows);
        return Convert.ToInt32(rows[0]["ServerID"]);
    }

    private DataRow GetSingleReservationRowByLabId(string labId)
    {
        DataTable result = _gateway.GetAllReservations();
        DataRow[] rows = result.Select($"LabID = '{labId.Replace("'", "''")}'");

        Assert.Single(rows);
        return rows[0];
    }

    private int GetSingleReservationIdByLabId(string labId)
    {
        DataRow row = GetSingleReservationRowByLabId(labId);
        return Convert.ToInt32(row["ReservationID"]);
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

    private static bool TableContainsReservation(DataTable table, int userId, string labId, int serverId, DateTime startTime, DateTime endTime)
    {
        foreach (DataRow row in table.Rows)
        {
            if (Convert.ToInt32(row["UserID"]) == userId
                && Convert.ToInt32(row["ServerID"]) == serverId
                && string.Equals(Convert.ToString(row["LabID"]), labId, StringComparison.Ordinal)
                && Convert.ToDateTime(row["StartDate"]) == startTime
                && Convert.ToDateTime(row["EndDate"]) == endTime)
            {
                return true;
            }
        }

        return false;
    }
}