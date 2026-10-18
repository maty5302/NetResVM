using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Interface;
using Moq;
using SimpleLogger;
using System.Data;
using System.Text;
using Xunit;

namespace NetResVM.UnitTests
{
    public class UserImportServiceTests
    {
        private readonly Mock<IUserTableDataGateway> _mockGateway;
        private readonly Mock<ILogger> _mockLogger;
        private readonly UserService _userService;
        private readonly UserImportService _importService;

        public UserImportServiceTests()
        {
            _mockGateway = new Mock<IUserTableDataGateway>();
            _mockLogger = new Mock<ILogger>();
            _userService = new UserService(_mockGateway.Object);
            _importService = new UserImportService(_userService, _mockLogger.Object);
        }

        private static DataTable CreateEmptyUserTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("UserID", typeof(int));
            dt.Columns.Add("Username", typeof(string));
            dt.Columns.Add("Password", typeof(string));
            dt.Columns.Add("Role", typeof(string));
            dt.Columns.Add("AuthorizationType", typeof(string));
            dt.Columns.Add("Active", typeof(int));
            return dt;
        }

        private static DataTable CreateUserTableWithUser(string username)
        {
            var dt = CreateEmptyUserTable();
            dt.Rows.Add(1, username, null, "student", "vsb", 1);
            return dt;
        }

        private static MemoryStream CreateStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        #region IsValidUsername Tests

        [Theory]
        [InlineData("nov0123", true)]
        [InlineData("ab", true)]
        [InlineData("john_doe", true)]
        [InlineData("john-doe", true)]
        [InlineData("john.doe", true)]
        [InlineData("user1234567890123456789012345678901234567890123456", true)] // 50 chars
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        [InlineData("a", false)] // too short (< 2)
        [InlineData("user12345678901234567890123456789012345678901234567", false)] // 51 chars (> 50)
        [InlineData("nov 123", false)] // spaces
        [InlineData("nov@123", false)] // invalid symbol
        [InlineData("žluťoučký", false)] // non-ascii accented chars
        [InlineData("user#1", false)]
        public void IsValidUsername_ValidatesCorrectly(string? username, bool expected)
        {
            bool actual = UserImportService.IsValidUsername(username!);
            Assert.Equal(expected, actual);
        }

        #endregion

        #region ExtractUsernamesFromStream Tests

        [Fact]
        public void ExtractUsernamesFromStream_UnsupportedExtension_ReturnsEmpty()
        {
            using var stream = CreateStream("nov0123\nnov0124");
            var result = _importService.ExtractUsernamesFromStream(stream, "students.xlsx");

            Assert.Empty(result);
            _mockLogger.Verify(l => l.LogWarning(It.Is<string>(s => s.Contains("Unsupported file extension"))), Times.Once);
        }

        [Fact]
        public void ExtractUsernamesFromStream_TxtFile_ParsesValidLoginsAndSkipsComments()
        {
            string content = @"
# This is a comment
// Another comment
nov0123
nov0124

   nov0125   
nov0123 # duplicate
invalid!login
nov0126@vsb.cz
";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "students.txt");

            Assert.Equal(4, result.Count);
            Assert.Contains("nov0123", result);
            Assert.Contains("nov0124", result);
            Assert.Contains("nov0125", result);
            Assert.Contains("nov0126", result);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvWithComma_ParsesHeaderAndLogins()
        {
            string content = "Login,FullName,Status\nnov0123,Jan Novak,Active\nnov0124,Petr Svoboda,Active";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "students.csv");

            Assert.Equal(2, result.Count);
            Assert.Equal("nov0123", result[0]);
            Assert.Equal("nov0124", result[1]);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvWithSemicolonAndCzechHeader_ParsesCorrectColumn()
        {
            string content = "Jméno;Příjmení;Osobní číslo;Stav\nJan;Novák;nov0123;Aktivní\nPetr;Svoboda;svo0099;Aktivní";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "edison_export.csv");

            Assert.Equal(2, result.Count);
            Assert.Equal("nov0123", result[0]);
            Assert.Equal("svo0099", result[1]);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvWithTab_ParsesCorrectColumn()
        {
            string content = "ID\tUsername\tGroup\n1\tuser_one\tGroupA\n2\tuser_two\tGroupB";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "users.csv");

            Assert.Equal(2, result.Count);
            Assert.Equal("user_one", result[0]);
            Assert.Equal("user_two", result[1]);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvWithoutHeader_UsesFirstColumn()
        {
            string content = "nov0123,other1\nnov0124,other2\nnov0125,other3";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "no_header.csv");

            Assert.Equal(3, result.Count);
            Assert.Equal("nov0123", result[0]);
            Assert.Equal("nov0124", result[1]);
            Assert.Equal("nov0125", result[2]);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvEmptyFile_ReturnsEmpty()
        {
            using var stream = CreateStream("");
            var result = _importService.ExtractUsernamesFromStream(stream, "empty.csv");

            Assert.Empty(result);
        }

        [Fact]
        public void ExtractUsernamesFromStream_CsvWithQuotesAndEmails_StripsQuotesAndEmails()
        {
            string content = "\"Login\";\"Email\"\n\"nov0123@vsb.cz\";\"nov0123@vsb.cz\"\n\"nov0124\";\"nov0124@vsb.cz\"";
            using var stream = CreateStream(content);
            var result = _importService.ExtractUsernamesFromStream(stream, "export.csv");

            Assert.Equal(2, result.Count);
            Assert.Equal("nov0123", result[0]);
            Assert.Equal("nov0124", result[1]);
        }

        #endregion

        #region ImportUsersFromStream Tests

        [Fact]
        public void ImportUsersFromStream_AllNewUsers_ImportsSuccessfully()
        {
            // Setup gateway: none of the users exist in DB
            _mockGateway.Setup(g => g.GetUserByUsername(It.IsAny<string>()))
                .Returns(CreateEmptyUserTable());

            string content = "nov0123\nnov0124\nnov0125";
            using var stream = CreateStream(content);

            var result = _importService.ImportUsersFromStream(stream, "users.txt", role: "student", authType: "vsb", active: true);

            Assert.Equal(3, result.TotalFound);
            Assert.Equal(3, result.AddedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(0, result.InvalidCount);
            Assert.Equal(new[] { "nov0123", "nov0124", "nov0125" }, result.AddedUsernames);

            _mockGateway.Verify(g => g.AddUser("nov0123", "student", "vsb", 1), Times.Once);
            _mockGateway.Verify(g => g.AddUser("nov0124", "student", "vsb", 1), Times.Once);
            _mockGateway.Verify(g => g.AddUser("nov0125", "student", "vsb", 1), Times.Once);
        }

        [Fact]
        public void ImportUsersFromStream_ExistingUsers_AreSkippedWithoutError()
        {
            // Setup gateway: nov0123 exists, nov0124 does not
            _mockGateway.Setup(g => g.GetUserByUsername("nov0123"))
                .Returns(CreateUserTableWithUser("nov0123"));
            _mockGateway.Setup(g => g.GetUserByUsername("nov0124"))
                .Returns(CreateEmptyUserTable());

            string content = "nov0123\nnov0124";
            using var stream = CreateStream(content);

            var result = _importService.ImportUsersFromStream(stream, "users.txt", role: "student", authType: "vsb", active: true);

            Assert.Equal(2, result.TotalFound);
            Assert.Equal(1, result.AddedCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Contains("nov0123", result.SkippedUsernames);
            Assert.Contains("nov0124", result.AddedUsernames);

            // Verify nov0123 was NOT added
            _mockGateway.Verify(g => g.AddUser("nov0123", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            // Verify nov0124 was added
            _mockGateway.Verify(g => g.AddUser("nov0124", "student", "vsb", 1), Times.Once);
        }

        [Fact]
        public void ImportUsersFromStream_GatewayFailsToAdd_RecordsAsInvalid()
        {
            _mockGateway.Setup(g => g.GetUserByUsername(It.IsAny<string>()))
                .Returns(CreateEmptyUserTable());
            // Throw exception or fail on AddUser
            _mockGateway.Setup(g => g.AddUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Throws(new Exception("Database connection error"));

            string content = "nov0123";
            using var stream = CreateStream(content);

            var result = _importService.ImportUsersFromStream(stream, "users.txt");

            Assert.Equal(1, result.TotalFound);
            Assert.Equal(0, result.AddedCount);
            Assert.Equal(1, result.InvalidCount);
            Assert.Contains("nov0123", result.InvalidUsernames);
        }

        [Fact]
        public void ImportUsersFromStream_WithLocalhostAuthType_PassesNullPasswordAndFailsValidation()
        {
            // Localhost requires a password in UserService.AddUser; null password will be rejected
            _mockGateway.Setup(g => g.GetUserByUsername(It.IsAny<string>()))
                .Returns(CreateEmptyUserTable());

            string content = "localuser";
            using var stream = CreateStream(content);

            var result = _importService.ImportUsersFromStream(stream, "users.txt", role: "student", authType: "localhost");

            Assert.Equal(1, result.TotalFound);
            Assert.Equal(0, result.AddedCount);
            Assert.Equal(1, result.InvalidCount);
            Assert.Contains("localuser", result.InvalidUsernames);
        }

        [Fact]
        public void UserImportResult_DefaultLists_AreNotNull()
        {
            var result = new UserImportResult();
            Assert.NotNull(result.AddedUsernames);
            Assert.NotNull(result.SkippedUsernames);
            Assert.NotNull(result.InvalidUsernames);
            Assert.Equal(0, result.TotalFound);
            Assert.Equal(0, result.AddedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(0, result.InvalidCount);
        }

        [Fact]
        public void UserImportService_DefaultConstructor_InstantiatesSuccessfully()
        {
            var service = new UserImportService();
            Assert.NotNull(service);
        }

        #endregion
    }
}
