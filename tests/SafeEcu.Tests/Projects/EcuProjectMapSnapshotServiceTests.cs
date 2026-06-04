using Microsoft.Data.Sqlite;
using SafeEcu.Application.Projects;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectMapSnapshotServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuProjectMapSnapshotServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "test-safe-ecu.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task CreateSnapshotAsync_reads_uint8_cells_with_converted_values()
    {
        await InitializeDatabaseAsync();
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x01, 0x02, 0x03, 0x04]);
        var project = await CreateProjectAsync();
        await AddMapDefinitionAsync(project.Id, "map-2x2", "UInt8", "NotApplicable", factor: 2m);
        var service = new EcuProjectMapSnapshotService(new EcuProjectMapDefinitionRepository(_dbContextFactory));

        var result = await service.CreateSnapshotAsync(new EcuProjectMapSnapshotRequest(project.Id, "map-2x2", filePath));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(2, result.ColumnCount);
        Assert.Equal([1, 2, 3, 4], result.Cells.Select(cell => cell.RawValue));
        Assert.Equal([2m, 4m, 6m, 8m], result.Cells.Select(cell => cell.ConvertedValue));
        Assert.Contains(result.BlockReasons, reason => reason.Contains("read-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateSnapshotAsync_reads_big_endian_uint16_cells()
    {
        await InitializeDatabaseAsync();
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x01, 0x00, 0x02, 0x00]);
        var project = await CreateProjectAsync();
        await AddMapDefinitionAsync(project.Id, "uint16-map", "UInt16", "BigEndian", factor: 1m, length: 4, rows: 1, columns: 2);
        var service = new EcuProjectMapSnapshotService(new EcuProjectMapDefinitionRepository(_dbContextFactory));

        var result = await service.CreateSnapshotAsync(new EcuProjectMapSnapshotRequest(project.Id, "uint16-map", filePath));

        Assert.True(result.IsSuccess);
        Assert.Equal([256, 512], result.Cells.Select(cell => cell.RawValue));
    }

    [Fact]
    public async Task CreateSnapshotAsync_blocks_map_range_outside_file()
    {
        await InitializeDatabaseAsync();
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x01, 0x02]);
        var project = await CreateProjectAsync();
        await AddMapDefinitionAsync(project.Id, "too-large", "UInt8", "NotApplicable", factor: 1m);
        var service = new EcuProjectMapSnapshotService(new EcuProjectMapDefinitionRepository(_dbContextFactory));

        var result = await service.CreateSnapshotAsync(new EcuProjectMapSnapshotRequest(project.Id, "too-large", filePath));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("outside", StringComparison.OrdinalIgnoreCase));
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

    private async Task<EcuProject> CreateProjectAsync()
    {
        var vehicle = new Vehicle { Make = "Renault", Model = "Megane 3" };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);
        var original = new EcuFile
        {
            VehicleId = vehicle.Id,
            FileType = EcuFileType.Original,
            FileName = "original.bin",
            FilePath = "backups/original.bin",
            SizeBytes = 4,
            Sha256Hash = new string('c', 64),
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
            "Snapshot project",
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
        return projectResult.Value!;
    }

    private async Task AddMapDefinitionAsync(
        Guid projectId,
        string mapId,
        string dataType,
        string endianess,
        decimal factor,
        int length = 4,
        int rows = 2,
        int columns = 2)
    {
        var definition = new EcuProjectMapDefinition
        {
            ProjectId = projectId,
            MapId = mapId,
            DisplayName = "Snapshot map",
            ParameterName = "Unknown",
            Category = "Candidate Maps",
            StartOffset = 0,
            Length = length,
            RowCount = rows,
            ColumnCount = columns,
            DataType = dataType,
            Endianess = endianess,
            Factor = factor,
            Unit = "raw",
            RequiredMapIds = string.Empty,
            ProfileId = "manual",
            SupportStatus = "Unknown"
        };

        await new EcuProjectMapDefinitionRepository(_dbContextFactory).AddAsync(definition);
    }
}
