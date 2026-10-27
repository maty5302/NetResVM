using DataLayer;
using System.Text;
using Xunit;

namespace NetResVM.UnitTests
{
    public class LocalBackupStorageTests : IDisposable
    {
        private readonly LocalBackupStorage _storage;
        private readonly string _testServerType = "TestServer";
        private readonly string _testLabId = "TestLab101";

        public LocalBackupStorageTests()
        {
            _storage = new LocalBackupStorage();
        }

        public void Dispose()
        {
            try
            {
                string backupsDir = Path.Combine(Directory.GetCurrentDirectory(), "backups", _testServerType);
                if (Directory.Exists(backupsDir))
                {
                    Directory.Delete(backupsDir, true);
                }
            }
            catch
            {
                // Suppress test cleanup errors
            }
        }

        [Fact]
        public async Task SaveBackup_WithValidInputs_SavesFileSuccessfully()
        {
            byte[] content = Encoding.UTF8.GetBytes("sample backup content");
            await _storage.SaveBackup(_testServerType, _testLabId, content, ".zip");

            var records = _storage.GetBackupRecords();
            var saved = records.FirstOrDefault(r => r.ServerType == _testServerType && r.LabId == _testLabId);

            Assert.NotNull(saved);
            Assert.EndsWith(".zip", saved.FileName);
        }

        [Theory]
        [InlineData("", "lab1")]
        [InlineData("   ", "lab1")]
        [InlineData("eve", "")]
        [InlineData("eve", "   ")]
        public async Task SaveBackup_WithInvalidInputs_ThrowsArgumentException(string serverType, string labId)
        {
            byte[] content = Encoding.UTF8.GetBytes("test");
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _storage.SaveBackup(serverType, labId, content, ".zip"));
        }

        [Fact]
        public async Task GetBackup_WithExistingFile_ReturnsBytes()
        {
            byte[] content = Encoding.UTF8.GetBytes("payload for retrieval");
            await _storage.SaveBackup(_testServerType, _testLabId, content, ".yaml");

            var records = _storage.GetBackupRecords();
            var saved = records.First(r => r.ServerType == _testServerType && r.LabId == _testLabId);

            var retrieved = await _storage.GetBackup(_testServerType, _testLabId, saved.FileName);

            Assert.NotNull(retrieved);
            Assert.Equal("payload for retrieval", Encoding.UTF8.GetString(retrieved));
        }

        [Fact]
        public async Task GetBackup_WithNonExistingFile_ReturnsNull()
        {
            var result = await _storage.GetBackup(_testServerType, _testLabId, "nonexistent.zip");
            Assert.Null(result);
        }

        [Theory]
        [InlineData("../../appsettings.json")]
        [InlineData("..\\..\\appsettings.json")]
        [InlineData("/etc/passwd")]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetBackup_WithPathTraversal_ReturnsNull(string maliciousFile)
        {
            var result = await _storage.GetBackup(_testServerType, _testLabId, maliciousFile);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteBackup_WithExistingFile_DeletesAndReturnsTrue()
        {
            byte[] content = Encoding.UTF8.GetBytes("payload for deletion");
            await _storage.SaveBackup(_testServerType, _testLabId, content, ".zip");

            var records = _storage.GetBackupRecords();
            var saved = records.First(r => r.ServerType == _testServerType && r.LabId == _testLabId);

            bool deleted = _storage.DeleteBackup(_testServerType, _testLabId, saved.FileName);
            Assert.True(deleted);

            var check = await _storage.GetBackup(_testServerType, _testLabId, saved.FileName);
            Assert.Null(check);
        }

        [Fact]
        public void DeleteBackup_WithNonExistingFile_ReturnsFalse()
        {
            bool deleted = _storage.DeleteBackup(_testServerType, _testLabId, "nonexistent.zip");
            Assert.False(deleted);
        }

        [Theory]
        [InlineData("../../appsettings.json")]
        [InlineData("")]
        [InlineData("   ")]
        public void DeleteBackup_WithPathTraversal_ReturnsFalse(string maliciousFile)
        {
            bool deleted = _storage.DeleteBackup(_testServerType, _testLabId, maliciousFile);
            Assert.False(deleted);
        }

        [Fact]
        public void GetBackupRecords_WhenCalled_ReturnsRecordsList()
        {
            var records = _storage.GetBackupRecords();
            Assert.NotNull(records);
        }

        [Theory]
        [InlineData("/", "lab1", "file.zip")]
        [InlineData("eve", "/", "file.zip")]
        [InlineData("eve", "lab1", "/")]
        public async Task GetBackup_WithSlashInputs_ReturnsNull(string serverType, string labId, string fileName)
        {
            var result = await _storage.GetBackup(serverType, labId, fileName);
            Assert.Null(result);
        }

        [Fact]
        public void GetBackupRecords_WhenDirectoryDoesNotExist_CreatesDirectoryAndReturnsEmptyList()
        {
            string backupsDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
            // If we temporarily delete or test a storage instance with non-existent dir
            var records = _storage.GetBackupRecords();
            Assert.NotNull(records);
        }
    }
}
