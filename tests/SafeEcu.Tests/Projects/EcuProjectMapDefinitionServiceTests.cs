using Microsoft.Data.Sqlite;
using SafeEcu.Application.Common;
using SafeEcu.Application.Projects;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectMapDefinitionServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuProjectMapDefinitionServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "test-safe-ecu.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task CreateAsync_saves_manual_map_definition_for_project()
    {
        await InitializeDatabaseAsync();
        var project = await CreateProjectAsync();
        var service = CreateMapDefinitionService();

        var result = await service.CreateAsync(CreateRequest(project.Id, "driver-request-candidate"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Candidate Maps", result.Value.Category);
        Assert.Equal(0x1200, result.Value.StartOffset);
        Assert.Equal(16, result.Value.RowCount);
        Assert.Equal(16, result.Value.ColumnCount);

        var definitions = await service.ListByProjectAsync(project.Id);
        Assert.Single(definitions);
        Assert.Equal("driver-request-candidate", definitions[0].MapId);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_map_id_in_same_project()
    {
        await InitializeDatabaseAsync();
        var project = await CreateProjectAsync();
        var service = CreateMapDefinitionService();

        var first = await service.CreateAsync(CreateRequest(project.Id, "duplicate-map"));
        var duplicate = await service.CreateAsync(CreateRequest(project.Id, "duplicate-map"));

        Assert.True(first.IsSuccess);
        Assert.False(duplicate.IsSuccess);
        Assert.Contains("same id", duplicate.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_blocks_emissions_related_map_definitions()
    {
        await InitializeDatabaseAsync();
        var project = await CreateProjectAsync();
        var service = CreateMapDefinitionService();

        var result = await service.CreateAsync(CreateRequest(project.Id, "blocked-emissions", isEmissionsRelated: true));

        Assert.False(result.IsSuccess);
        Assert.Contains("Emissions-related", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_range_and_factor()
    {
        await InitializeDatabaseAsync();
        var project = await CreateProjectAsync();
        var service = CreateMapDefinitionService();

        var result = await service.CreateAsync(CreateRequest(
            project.Id,
            "invalid-map",
            startOffset: -1,
            length: 0,
            factor: 0));

        Assert.False(result.IsSuccess);
        Assert.Contains("address range", result.ErrorMessage);
        Assert.Contains("factor", result.ErrorMessage);
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

    private EcuProjectMapDefinitionService CreateMapDefinitionService() =>
        new(
            new EcuProjectRepository(_dbContextFactory),
            new EcuProjectMapDefinitionRepository(_dbContextFactory),
            _logger);

    private async Task<EcuProject> CreateProjectAsync()
    {
        var vehicle = new Vehicle
        {
            Make = "Renault",
            Model = "Megane 3",
            Engine = "1.5 dCi",
            EngineCode = "K9K",
            FuelType = FuelType.Diesel
        };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);

        var original = new EcuFile
        {
            VehicleId = vehicle.Id,
            FileType = EcuFileType.Original,
            FileName = "original.bin",
            FilePath = "backups/original.bin",
            SizeBytes = 4096,
            Sha256Hash = new string('a', 64),
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
            "Megane project",
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

    private static EcuProjectMapDefinitionCreateRequest CreateRequest(
        Guid projectId,
        string mapId,
        bool isEmissionsRelated = false,
        long startOffset = 0x1200,
        int length = 256,
        decimal factor = 1m) =>
        new(
            projectId,
            mapId,
            "Manual candidate map",
            "Unknown manual candidate",
            "Candidate Maps",
            startOffset,
            length,
            16,
            16,
            "UInt8",
            "NotApplicable",
            factor,
            0m,
            "raw",
            null,
            null,
            null,
            null,
            null,
            null,
            0m,
            255m,
            isEmissionsRelated,
            string.Empty,
            "manual-project-profile",
            "Unknown",
            "User-defined candidate. Not verified for calibration output.");
}
