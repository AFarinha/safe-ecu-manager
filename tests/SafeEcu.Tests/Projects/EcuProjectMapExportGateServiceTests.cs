using Microsoft.Data.Sqlite;
using SafeEcu.Application.Projects;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectMapExportGateServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuProjectMapExportGateServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "test-safe-ecu.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task EvaluateAsync_blocks_export_when_checksum_is_not_supported()
    {
        await InitializeDatabaseAsync();
        var projectId = await CreateProjectIdAsync();
        var service = new EcuProjectMapExportGateService(new EcuProjectRepository(_dbContextFactory));

        var result = await service.EvaluateAsync(CreateRequest(projectId, checksumSupported: false));

        Assert.False(result.CanExport);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Checksum algorithm is not supported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_blocks_export_when_support_status_is_not_verified()
    {
        await InitializeDatabaseAsync();
        var projectId = await CreateProjectIdAsync();
        var service = new EcuProjectMapExportGateService(new EcuProjectRepository(_dbContextFactory));

        var result = await service.EvaluateAsync(CreateRequest(projectId, supportStatus: "Unknown"));

        Assert.False(result.CanExport);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("not Verified", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_allows_export_only_when_all_gates_pass()
    {
        await InitializeDatabaseAsync();
        var projectId = await CreateProjectIdAsync();
        var service = new EcuProjectMapExportGateService(new EcuProjectRepository(_dbContextFactory));

        var result = await service.EvaluateAsync(CreateRequest(projectId));

        Assert.True(result.CanExport);
        Assert.Contains(result.Messages, message => message.Contains("does not allow ECU writing", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private async Task<Guid> CreateProjectIdAsync()
    {
        var vehicle = new Vehicle { Make = "Renault", Model = "Megane 3" };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);
        var original = new EcuFile
        {
            VehicleId = vehicle.Id,
            FileType = EcuFileType.Original,
            FileName = "original.bin",
            FilePath = "backups/original.bin",
            SizeBytes = 1024,
            Sha256Hash = new string('d', 64),
            ChecksumStatus = ChecksumStatus.Unknown
        };
        await new EcuFileRepository(_dbContextFactory).AddAsync(original);

        var projectService = new EcuProjectService(
            new EcuProjectRepository(_dbContextFactory),
            new EcuFileRepository(_dbContextFactory),
            _logger);
        var projectResult = await projectService.CreateAsync(new EcuProjectCreateRequest(
            vehicle.Id,
            original.Id,
            "Export gate project",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            null,
            "Unknown",
            null,
            "External read",
            null,
            null));

        Assert.True(projectResult.IsSuccess);
        return projectResult.Value!.Id;
    }

    private static EcuProjectMapExportGateRequest CreateRequest(
        Guid projectId,
        bool checksumSupported = true,
        string supportStatus = "Verified") =>
        new(
            projectId,
            new EcuProjectMapEditPreviewResult(
                true,
                [new EcuProjectMapEditPreviewCell(0, 0, 0, 10m, 11m, 1m)],
                ["Preview ready."],
                ["Preview is in-memory only."]),
            checksumSupported,
            ChecksumWillBeRecalculated: true,
            ExplicitUserConfirmation: true,
            supportStatus);
}
