using System.Data;
using DataLayer;

namespace NetResVM.IntegrationTests
{
    [Trait("Category", "Integration")]
    [Collection("Integration DB")]
    public class UserTableDataGatewayTests
    {
        private readonly UserTableDataGateway _userTableDataGateway;

        public UserTableDataGatewayTests(DatabaseFixture fixture)
        {
            DBConnector.TestConnectionString = fixture.ConnectionString;
            _userTableDataGateway = new UserTableDataGateway();
        }

        [Fact]
        public void AddUser_WithPassword_ShouldPersistAllFields()
        {
            // Arrange
            string username = CreateUniqueUsername("add-with-password");

            // Act
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);

            // Assert
            DataRow row = GetSingleUserRowByUsername(username);
            Assert.Equal(username, row["Username"]);
            Assert.Equal("testpassword", row["Password"]);
            Assert.Equal("student", row["Role"]);
            Assert.Equal("localhost", row["AuthorizationType"]);
            Assert.Equal(1, row["Active"]);
        }

        [Fact]
        public void AddUser_WithoutPassword_ShouldPersistNullPassword()
        {
            // Arrange
            string username = CreateUniqueUsername("add-without-password");

            // Act
            _userTableDataGateway.AddUser(username, "student", "vsb", 0);

            // Assert
            DataRow row = GetSingleUserRowByUsername(username);
            Assert.Equal(username, row["Username"]);
            Assert.True(row.IsNull("Password"));
            Assert.Equal("student", row["Role"]);
            Assert.Equal("vsb", row["AuthorizationType"]);
            Assert.Equal(0, row["Active"]);
        }

        [Fact]
        public void GetUserByUsername_WhenUserExists_ShouldReturnUser()
        {
            // Arrange
            string username = CreateUniqueUsername("get-by-username");
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);

            // Act
            DataTable result = _userTableDataGateway.GetUserByUsername(username);

            // Assert
            Assert.Single(result.Rows);
            Assert.Equal(username, result.Rows[0]["Username"]);
        }

        [Fact]
        public void GetUserByUsername_WhenUserDoesNotExist_ShouldReturnEmptyTable()
        {
            // Act
            DataTable result = _userTableDataGateway.GetUserByUsername(CreateUniqueUsername("missing-user"));

            // Assert
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void GetUserById_WhenUserExists_ShouldReturnUser()
        {
            // Arrange
            string username = CreateUniqueUsername("get-by-id");
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);
            int userId = GetSingleUserIdByUsername(username);

            // Act
            DataTable result = _userTableDataGateway.GetUserById(userId);

            // Assert
            Assert.Single(result.Rows);
            Assert.Equal(userId, result.Rows[0]["UserID"]);
            Assert.Equal(username, result.Rows[0]["Username"]);
        }

        [Fact]
        public void GetUserById_WhenUserDoesNotExist_ShouldReturnEmptyTable()
        {
            // Act
            DataTable result = _userTableDataGateway.GetUserById(int.MaxValue);

            // Assert
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void GetAllUsers_ShouldContainInsertedUser()
        {
            // Arrange
            string username = CreateUniqueUsername("get-all");
            int rowsBefore = _userTableDataGateway.GetAllUsers().Rows.Count;

            // Act
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);
            DataTable result = _userTableDataGateway.GetAllUsers();

            // Assert
            Assert.Equal(rowsBefore + 1, result.Rows.Count);
            Assert.True(TableContainsUsername(result, username));
        }

        [Fact]
        public void UpdateUserActive_ShouldPersistUpdatedValue()
        {
            // Arrange
            string username = CreateUniqueUsername("update-active");
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);
            int userId = GetSingleUserIdByUsername(username);

            // Act
            _userTableDataGateway.UpdateUserActive(userId, 0);

            // Assert
            DataRow row = GetSingleUserRowByUsername(username);
            Assert.Equal(0, row["Active"]);
        }

        [Fact]
        public void UpdateUserPassword_ShouldPersistUpdatedPassword()
        {
            // Arrange
            string username = CreateUniqueUsername("update-password");
            _userTableDataGateway.AddUser(username, "old-password", "student", "localhost", 1);
            int userId = GetSingleUserIdByUsername(username);

            // Act
            _userTableDataGateway.UpdateUserPassword(userId, "new-password");

            // Assert
            DataRow row = GetSingleUserRowByUsername(username);
            Assert.Equal("new-password", row["Password"]);
        }

        [Fact]
        public void RemoveUser_ShouldDeleteUserFromDatabase()
        {
            // Arrange
            string username = CreateUniqueUsername("remove-user");
            _userTableDataGateway.AddUser(username, "testpassword", "student", "localhost", 1);
            int userId = GetSingleUserIdByUsername(username);

            // Act
            _userTableDataGateway.RemoveUser(userId);

            // Assert
            DataTable byUsername = _userTableDataGateway.GetUserByUsername(username);
            DataTable byId = _userTableDataGateway.GetUserById(userId);

            Assert.Empty(byUsername.Rows);
            Assert.Empty(byId.Rows);
        }

        private static string CreateUniqueUsername(string prefix)
        {
            string value = $"{prefix}-{Guid.NewGuid():N}";
            return value.Substring(0, Math.Min(value.Length, 50));
        }

        private DataRow GetSingleUserRowByUsername(string username)
        {
            DataTable result = _userTableDataGateway.GetUserByUsername(username);

            Assert.Single(result.Rows);
            return result.Rows[0];
        }

        private int GetSingleUserIdByUsername(string username)
        {
            DataRow row = GetSingleUserRowByUsername(username);
            return Convert.ToInt32(row["UserID"]);
        }

        private static bool TableContainsUsername(DataTable table, string username)
        {
            foreach (DataRow row in table.Rows)
            {
                if (string.Equals(Convert.ToString(row["Username"]), username, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
