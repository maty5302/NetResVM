using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using DataLayer;
using DataLayer.Interface;
using Moq;

namespace NetResVM.UnitTests;

public class BackupServiceTests
{
    private readonly Mock<ILocalBackupStorage> _mockLocalBackupStorage;
    private readonly Mock<IServerService> _mockServerService;
    private readonly Mock<IPlatformManager> _mockPlatformManager;
    private readonly BackupService _backupService;
    
    public BackupServiceTests()
    {
        _mockLocalBackupStorage = new Mock<ILocalBackupStorage>();
        _mockServerService = new Mock<IServerService>();
        _mockPlatformManager = new Mock<IPlatformManager>();
        _backupService = new BackupService(_mockPlatformManager.Object, _mockServerService.Object, _mockLocalBackupStorage.Object); 
    }

    #region BackupLab Tests

    [Fact]
    public async Task BackupLab_WithNonExistentServer_ReturnsFalse()
    {
        _mockServerService.Setup(s => s.GetServerById(99)).Returns((ServerDTO?)null);

        var result = await _backupService.BackupLab(99, "lab-1");

        Assert.False(result.backup);
        Assert.Equal("Server not found", result.Message);
    }

    [Fact]
    public async Task BackupLab_WithUnknownPlatform_ReturnsFalse()
    {
        var server = new ServerDTO
        {
            Id = 1,
            Name = "Server",
            Platform = PlatformType.Unknown,
            IpAddress = "127.0.0.1",
            Username = "admin"
        };
        _mockServerService.Setup(s => s.GetServerById(server.Id)).Returns(server);

        var result = await _backupService.BackupLab(server.Id, "lab-1");

        Assert.False(result.backup);
        Assert.Equal("Unknown platform", result.Message);
    }

    [Fact]
    public async Task BackupLab_WhenLabNotFound_ReturnsFalse()
    {
        var server = CreateServer(1, PlatformType.CML);
        var adapter = new Mock<IVirtualizationAdapter>();

        _mockServerService.Setup(s => s.GetServerById(server.Id)).Returns(server);
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.GetLabInfoAsync(server.Id, "lab-1")).ReturnsAsync(((LabDTO?)null, "not found"));

        var result = await _backupService.BackupLab(server.Id, "lab-1");

        Assert.False(result.backup);
        Assert.Equal("Lab not found", result.Message);
        _mockLocalBackupStorage.Verify(s => s.SaveBackup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BackupLab_WhenDownloadFails_ReturnsFalse()
    {
        var server = CreateServer(1, PlatformType.CML);
        var lab = new LabDTO
        {
            Id = "lab-1",
            Name = "Lab",
            Description = string.Empty,
            Status = "Stopped"
        };
        var adapter = new Mock<IVirtualizationAdapter>();

        _mockServerService.Setup(s => s.GetServerById(server.Id)).Returns(server);
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.GetLabInfoAsync(server.Id, lab.Id)).ReturnsAsync((lab, string.Empty));
        adapter.Setup(a => a.DownloadLab(server.Id, lab.Id, It.IsAny<LabDTO>())).ReturnsAsync((null, string.Empty, string.Empty, "fail"));

        var result = await _backupService.BackupLab(server.Id, lab.Id);

        Assert.False(result.backup);
        Assert.Equal("Backup failed", result.Message);
        _mockLocalBackupStorage.Verify(s => s.SaveBackup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BackupLab_WhenDownloadSucceeds_ReturnsTrueAndSavesBackup()
    {
        var server = CreateServer(1, PlatformType.CML);
        var lab = new LabDTO
        {
            Id = "lab-1",
            Name = "Lab",
            Description = string.Empty,
            Status = "Running"
        };
        var adapter = new Mock<IVirtualizationAdapter>();
        var content = new byte[] { 1, 2, 3 };

        _mockServerService.Setup(s => s.GetServerById(server.Id)).Returns(server);
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.GetLabInfoAsync(server.Id, lab.Id)).ReturnsAsync((lab, string.Empty));
        adapter.Setup(a => a.DownloadLab(server.Id, lab.Id, It.IsAny<LabDTO>())).ReturnsAsync((content, "application/zip", "lab.zip", string.Empty));

        var result = await _backupService.BackupLab(server.Id, lab.Id);

        Assert.True(result.backup);
        Assert.Equal("Backup successful", result.Message);
        _mockLocalBackupStorage.Verify(s => s.SaveBackup("CML", lab.Id, content, ".yaml"), Times.Once);
    }

    #endregion

    #region GetBackups Tests

    [Fact]
    public async Task GetBackups_WhenNoBackups_ReturnsEmptyList()
    {
        _mockLocalBackupStorage.Setup(s => s.GetBackupRecords()).Returns(new List<BackupRecord>());
        _mockServerService.Setup(s => s.GetAllServers()).Returns(new List<ServerDTO>());

        var result = await _backupService.GetBackups();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBackups_WhenLabExistsOnServer_ReturnsMappedServerInfo()
    {
        var backupRecord = new BackupRecord
        {
            ServerType = "CML",
            LabId = "lab-1",
            FileName = "lab-1.zip",
            FullPath = "/tmp/lab-1.zip",
            CreatedAt = DateTime.UtcNow
        };
        var server = CreateServer(7, PlatformType.CML);
        var adapter = new Mock<IVirtualizationAdapter>();

        _mockLocalBackupStorage.Setup(s => s.GetBackupRecords()).Returns(new List<BackupRecord> { backupRecord });
        _mockServerService.Setup(s => s.GetAllServers()).Returns(new List<ServerDTO> { server });
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.AuthenticateAsync(server.Id)).ReturnsAsync((true, string.Empty));
        adapter.Setup(a => a.GetLabInfoAsync(server.Id, backupRecord.LabId)).ReturnsAsync((new LabDTO { Id = backupRecord.LabId }, string.Empty));

        var result = await _backupService.GetBackups();

        Assert.Single(result);
        Assert.Equal(server.Id, result[0].ServerId);
        Assert.Equal(server.Name, result[0].ServerName);
        Assert.Equal(backupRecord.FileName, result[0].FileName);
    }

    [Fact]
    public async Task GetBackups_WhenLabNotFoundOnAnyServer_ReturnsUnknownServer()
    {
        var backupRecord = new BackupRecord
        {
            ServerType = "CML",
            LabId = "lab-x",
            FileName = "lab-x.zip",
            FullPath = "/tmp/lab-x.zip",
            CreatedAt = DateTime.UtcNow
        };
        var server = CreateServer(8, PlatformType.CML);
        var adapter = new Mock<IVirtualizationAdapter>();

        _mockLocalBackupStorage.Setup(s => s.GetBackupRecords()).Returns(new List<BackupRecord> { backupRecord });
        _mockServerService.Setup(s => s.GetAllServers()).Returns(new List<ServerDTO> { server });
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.AuthenticateAsync(server.Id)).ReturnsAsync((true, string.Empty));
        adapter.Setup(a => a.GetLabInfoAsync(server.Id, backupRecord.LabId)).ReturnsAsync(((LabDTO?)null, "not found"));

        var result = await _backupService.GetBackups();

        Assert.Single(result);
        Assert.Equal(-1, result[0].ServerId);
        Assert.Equal("Unknown", result[0].ServerName);
        Assert.Equal(PlatformType.CML, result[0].Platform);
    }

    #endregion

    #region RestoreBackup Tests

    [Fact]
    public async Task RestoreBackup_WhenBackupIsMissing_ReturnsFalse()
    {
        _mockLocalBackupStorage
            .Setup(s => s.GetBackup("CML", "lab-1", "backup.zip"))
            .ReturnsAsync((byte[]?)null);

        var result = await _backupService.RestoreBackup(1, PlatformType.CML, "lab-1", "backup.zip");

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreBackup_WhenAuthFails_ReturnsFalse()
    {
        var adapter = new Mock<IVirtualizationAdapter>();

        _mockLocalBackupStorage
            .Setup(s => s.GetBackup("CML", "lab-1", "backup.zip"))
            .ReturnsAsync(new byte[] { 1, 2, 3 });
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.AuthenticateAsync(1)).ReturnsAsync((false, "denied"));

        var result = await _backupService.RestoreBackup(1, PlatformType.CML, "lab-1", "backup.zip");

        Assert.False(result);
        _mockLocalBackupStorage.Verify(s => s.DeleteBackup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestoreBackup_WhenImportSucceeds_ReturnsTrueAndDeletesBackup()
    {
        var adapter = new Mock<IVirtualizationAdapter>();
        var content = new byte[] { 1, 2, 3 };

        _mockLocalBackupStorage.Setup(s => s.GetBackup("CML", "lab-1", "backup.zip")).ReturnsAsync(content);
        _mockLocalBackupStorage.Setup(s => s.DeleteBackup("CML", "lab-1", "backup.zip")).Returns(true);
        _mockPlatformManager.Setup(p => p.GetAdapter(PlatformType.CML)).Returns(adapter.Object);
        adapter.Setup(a => a.AuthenticateAsync(1)).ReturnsAsync((true, string.Empty));
        adapter.Setup(a => a.ImportLab(1, content, "backup.zip")).ReturnsAsync(true);

        var result = await _backupService.RestoreBackup(1, PlatformType.CML, "lab-1", "backup.zip");

        Assert.True(result);
        _mockLocalBackupStorage.Verify(s => s.DeleteBackup("CML", "lab-1", "backup.zip"), Times.Once);
    }

    #endregion

    #region DeleteBackup Tests

    [Fact]
    public void DeleteBackup_WhenStorageDeleteSucceeds_ReturnsTrue()
    {
        _mockLocalBackupStorage.Setup(s => s.DeleteBackup("CML", "lab-1", "backup.zip")).Returns(true);

        var result = _backupService.DeleteBackup("backup.zip", "lab-1", PlatformType.CML);

        Assert.True(result);
        _mockLocalBackupStorage.Verify(s => s.DeleteBackup("CML", "lab-1", "backup.zip"), Times.Once);
    }

    [Fact]
    public void DeleteBackup_WhenStorageDeleteFails_ReturnsFalse()
    {
        _mockLocalBackupStorage.Setup(s => s.DeleteBackup("CML", "lab-1", "backup.zip")).Returns(false);

        var result = _backupService.DeleteBackup("backup.zip", "lab-1", PlatformType.CML);

        Assert.False(result);
        _mockLocalBackupStorage.Verify(s => s.DeleteBackup("CML", "lab-1", "backup.zip"), Times.Once);
    }

    #endregion

    #region DownloadBackup Tests

    [Fact]
    public async Task DownloadBackup_WhenBackupExists_ReturnsContent()
    {
        var content = new byte[] { 4, 5, 6 };
        _mockLocalBackupStorage.Setup(s => s.GetBackup("CML", "lab-1", "backup.zip")).ReturnsAsync(content);

        var result = await _backupService.DownloadBackup(PlatformType.CML, "lab-1", "backup.zip");

        Assert.Equal(content, result);
    }

    [Fact]
    public async Task DownloadBackup_WhenBackupMissing_ReturnsEmptyArray()
    {
        _mockLocalBackupStorage.Setup(s => s.GetBackup("CML", "lab-1", "backup.zip")).ReturnsAsync((byte[]?)null);

        var result = await _backupService.DownloadBackup(PlatformType.CML, "lab-1", "backup.zip");

        Assert.Empty(result);
    }

    #endregion
    private static ServerDTO CreateServer(int id, PlatformType platform)
    {
        return new ServerDTO
        {
            Id = id,
            Name = $"Server-{id}",
            Platform = platform,
            IpAddress = "127.0.0.1",
            Username = "admin"
        };
    }
}