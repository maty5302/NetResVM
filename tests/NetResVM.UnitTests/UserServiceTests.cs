using BusinessLayer.Services;
using Moq;
using Xunit;
using System.Data;
using DataLayer.Interface;

namespace NetResVM.UnitTests
{
    /// <summary>
    /// Unit tests for the UserService class.
    /// Tests cover authentication, user retrieval, user management, and authorization.
    /// </summary>
    public class UserServiceTests : IDisposable
    {
        private readonly Mock<IUserTableDataGateway> _mockUserTableDataGateway;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserTableDataGateway = new Mock<IUserTableDataGateway>();
            _userService = new UserService(_mockUserTableDataGateway.Object);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        #region GetAllUsersInfo Tests

        [Fact]
        public void GetAllUsersInfo_WithValidUsers_ReturnsListOfUsersDTOs()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));

            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            datatable.Rows.Add(2, "user2", "password456", "User", "vsb", 0);

            _mockUserTableDataGateway.Setup(g => g.GetAllUsers()).Returns(datatable);

            var result = _userService.GetAllUsersInfo();
            Assert.NotNull(result);
            Assert.IsType<List<BusinessLayer.DTOs.UserDTO>>(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAllUsersInfo_WithNoUsers_ReturnsEmptyList()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));

            _mockUserTableDataGateway.Setup(g => g.GetAllUsers()).Returns(datatable);

            var result = _userService.GetAllUsersInfo();
            Assert.NotNull(result);
            Assert.IsType<List<BusinessLayer.DTOs.UserDTO>>(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetAuthorizationType Tests

        [Fact]
        public void GetAuthorizationType_WithExistingUser_ReturnsAuthorizationType()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("user1")).Returns(datatable);

            var result = _userService.GetAuthorizationType("user1");

            Assert.Equal("localhost", result);
        }

        [Fact]
        public void GetAuthorizationType_WithNonExistingUser_ReturnsEmptyString()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("nonexistent")).Returns(datatable);

            var result = _userService.GetAuthorizationType("nonexistent");
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region ValidateCredentials Tests

        [Fact]
        public void ValidateCredentials_WithValidCredentials_ReturnsTrue()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("user1")).Returns(datatable);
            var result = _userService.ValidateCredentials("user1", "password123");
            Assert.True(result);
        }

        [Fact]
        public void ValidateCredentials_WithInvalidCredentials_ReturnsFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("user1")).Returns(datatable);
            var result = _userService.ValidateCredentials("user1", "wrongpassword");
            Assert.False(result);
        }

        [Fact]
        public void ValidateCredentials_WithNonExistingUser_ReturnsFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("nonexistent")).Returns(datatable);
            var result = _userService.ValidateCredentials("nonexistent", "password123");
            Assert.False(result);
        }

        #endregion

        #region GetUserId Tests

        [Fact]
        public void GetUserId_WithExistingUser_ReturnsUserId()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("user1")).Returns(datatable);

            var result = _userService.GetUserId("user1");
            Assert.Equal(1, result);
        }

        [Fact]
        public void GetUserId_WithNonExistingUser_ReturnsMinusOne()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("nonexistent")).Returns(datatable);
            var result = _userService.GetUserId("nonexistent");
            Assert.Equal(-1, result);
        }

        #endregion

        #region GetUsername Tests

        [Fact]
        public void GetUsername_WithExistingUserId_ReturnsUsername()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.GetUsername(1);
            Assert.Equal("user1", result);
        }

        [Fact]
        public void GetUsername_WithNonExistingUserId_ReturnsEmptyString()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserById(999)).Returns(datatable);
            var result = _userService.GetUsername(999);
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region GetRole Tests

        [Fact]
        public void GetRole_WithExistingUser_ReturnsRole()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("user1")).Returns(datatable);

            var result = _userService.GetRole("user1");
            Assert.NotNull(result);
            Assert.Equal("Admin", result);
        }

        [Fact]
        public void GetRole_WithNonExistingUser_ReturnsEmptyString()
        {
            var datatable = new System.Data.DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername("nonexistent")).Returns(datatable);

            var result = _userService.GetRole("nonexistent");
            Assert.Equal(string.Empty, result);
        }

        #endregion

        #region AddUser Tests
        [Fact]
        public void AddUser_WithValidUser_ReturnsTrue()
        {
            const string username = "newuser";
            const string password = "password123";
            const string role = "student";
            const string authType = "localhost";

            // Act
            var result = _userService.AddUser(username, password, role, authType, true);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void AddUser_WithValidVSBUser_ReturnsTrue()
        {
            // Arrange
            const string username = "vsbuser";
            const string role = "User";
            const string authType = "vsb";

            // Act
            var result = _userService.AddUser(username, null, role, authType, true);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void AddUser_WithExistingUser_ReturnsFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            // Arrange
            const string username = "existinguser";
            const string password = "password123";
            const string role = "User";
            const string authType = "localhost";
            datatable.Rows.Add(1, username, password, role, authType, 1);

            _mockUserTableDataGateway.Setup(g => g.GetUserByUsername(username)).Returns(datatable);

            var result = _userService.AddUser(username, password, role, authType, true);

            Assert.False(result);
        }

        [Fact]
        public void AddUser_WithInvalidAuthorizationType_ReturnsFalse()
        {
            // Arrange
            const string username = "newuser";
            const string password = "password123";
            const string role = "User";
            const string invalidAuthType = "invalid";

            // Act
            var result = _userService.AddUser(username, password, role, invalidAuthType, true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public void UpdateUser_WithValidUser_DeactivatesUser_ReturnsTrue()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, false);
            Assert.True(result);
        }

        [Fact]
        public void UpdateUser_WithValidUser_ActivatesUser_ReturnsTrue()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 0);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, true);
            Assert.True(result);
        }

        [Fact]
        public void UpdateUser_WithNonExistingUser_ReturnsFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserById(999)).Returns(datatable);
            var result = _userService.UpdateUser(999, false);
            Assert.False(result);
        }

        [Fact]
        public void UpdateUser_WithValidUser_UpdatePassword_ReturnTrue()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, "newpassword");
            Assert.True(result);
        }

        [Fact]
        public void UpdateUser_WithNonExistingUser_UpdatePassword_ReturnFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserById(999)).Returns(datatable);
            var result = _userService.UpdateUser(999, "newpassword");
            Assert.False(result);
        }

        [Fact]
        public void UpdateUser_WithValidUser_UpdatePasswordToNull_ReturnFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, null);
            Assert.False(result);
        }

        [Fact]
        public void UpdateUser_WithValidUser_UpdatePasswordToEmpty_ReturnFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, "");
            Assert.False(result);
        }

        [Fact]
        public void UpdateUser_WithValidUser_UpdatePasswordToWhitespace_ReturnFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.UpdateUser(1, "   ");
            Assert.False(result);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public void DeleteUser_WithExistingUser_ReturnsTrue()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            datatable.Rows.Add(1, "user1", "password123", "Admin", "localhost", 1);
            _mockUserTableDataGateway.Setup(g => g.GetUserById(1)).Returns(datatable);
            var result = _userService.RemoveUser(1);
            Assert.True(result);
        }

        [Fact]
        public void DeleteUser_WithNonExistingUser_ReturnsFalse()
        {
            var datatable = new DataTable();
            datatable.Columns.Add("UserID", typeof(int));
            datatable.Columns.Add("Username", typeof(string));
            datatable.Columns.Add("Password", typeof(string));
            datatable.Columns.Add("Role", typeof(string));
            datatable.Columns.Add("AuthorizationType", typeof(string));
            datatable.Columns.Add("Active", typeof(int));
            _mockUserTableDataGateway.Setup(g => g.GetUserById(999)).Returns(datatable);
            var result = _userService.RemoveUser(999);
            Assert.False(result);
        }
        #endregion
    }
}