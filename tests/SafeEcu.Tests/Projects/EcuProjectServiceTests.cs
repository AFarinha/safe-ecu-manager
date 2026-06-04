using Microsoft.Data.Sqlite;
using SafeEcu.Application.Common;
using SafeEcu.Application.Projects;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuProjectServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "test-safe-ecu.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task CreateAsync_creates_project_with_protected_original_version()
    {
        await InitializeDatabaseAsync();
        var vehicle = await AddVehicleAsync();
        var original = await AddFileAsync(vehicle.Id, EcuFileType.Original, "original.bin", new string('1', 64));
        var service = CreateService();

        var result = await service.CreateAsync(new EcuProjectCreateRequest(
            vehicle.Id,
            original.Id,
            "Megane 3 K9K project",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            2011,
            "Unknown candidate",
            null,
            "OBD external read",
            "Baseline project",
            "renault,k9k"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var saved = await service.GetByIdAsync(result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal(original.Id, saved.OriginalFileId);
        Assert.Single(saved.Versions);
        Assert.Equal(EcuProjectFileVersionKind.Original, saved.Versions[0].Kind);
        Assert.True(saved.Versions[0].IsOriginalProtected);
    }

    [Fact]
    public async Task CreateAsync_rejects_non_original_file_as_protected_project_file()
    {
        await InitializeDatabaseAsync();
        var vehicle = await AddVehicleAsync();
        var modified = await AddFileAsync(vehicle.Id, EcuFileType.Modified, "modified.bin", new string('2', 64));
        var service = CreateService();

        var result = await service.CreateAsync(new EcuProjectCreateRequest(
            vehicle.Id,
            modified.Id,
            "Invalid project",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            null,
            "Unknown",
            null,
            "External read",
            null,
            null));

        Assert.False(result.IsSuccess);
        Assert.Contains("Original", result.ErrorMessage);
    }

    [Fact]
    public async Task AddVersionAsync_adds_modified_file_without_changing_original()
    {
        await InitializeDatabaseAsync();
        var vehicle = await AddVehicleAsync();
        var original = await AddFileAsync(vehicle.Id, EcuFileType.Original, "original.bin", new string('3', 64));
        var modified = await AddFileAsync(vehicle.Id, EcuFileType.Modified, "modified.bin", new string('4', 64));
        var service = CreateService();
        var projectResult = await CreateProjectAsync(service, vehicle.Id, original.Id);

        var result = await service.AddVersionAsync(new EcuProjectAddVersionRequest(
            projectResult.Value!.Id,
            modified.Id,
            EcuProjectFileVersionKind.Modified,
            "Controlled preview v2",
            "Generated after validation gates."));

        Assert.True(result.IsSuccess);

        var saved = await service.GetByIdAsync(projectResult.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal(original.Id, saved.OriginalFileId);
        Assert.Equal(2, saved.Versions.Count);
        Assert.Contains(saved.Versions, version => version.EcuFileId == original.Id && version.IsOriginalProtected);
        Assert.Contains(saved.Versions, version => version.EcuFileId == modified.Id && !version.IsOriginalProtected);
    }

    [Fact]
    public async Task AddVersionAsync_rejects_original_file_as_modified_version()
    {
        await InitializeDatabaseAsync();
        var vehicle = await AddVehicleAsync();
        var original = await AddFileAsync(vehicle.Id, EcuFileType.Original, "original.bin", new string('5', 64));
        var service = CreateService();
        var projectResult = await CreateProjectAsync(service, vehicle.Id, original.Id);

        var result = await service.AddVersionAsync(new EcuProjectAddVersionRequest(
            projectResult.Value!.Id,
            original.Id,
            EcuProjectFileVersionKind.Modified,
            "Unsafe reuse",
            null));

        Assert.False(result.IsSuccess);
        Assert.Contains("protected original", result.ErrorMessage);
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

    private EcuProjectService CreateService() =>
        new(new EcuProjectRepository(_dbContextFactory), new EcuFileRepository(_dbContextFactory), _logger);

    private async Task<Vehicle> AddVehicleAsync()
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
        return vehicle;
    }

    private async Task<EcuFile> AddFileAsync(Guid vehicleId, EcuFileType fileType, string fileName, string hash)
    {
        var file = new EcuFile
        {
            VehicleId = vehicleId,
            FileType = fileType,
            FileName = fileName,
            FilePath = $"backups/{fileName}",
            SizeBytes = 1024,
            Sha256Hash = hash,
            ReadMethod = "ManualWorkflowOnly",
            ProgrammerUsed = "FileOnly",
            ChecksumStatus = ChecksumStatus.Unknown
        };

        await new EcuFileRepository(_dbContextFactory).AddAsync(file);
        return file;
    }

    private static Task<OperationResult<EcuProject>> CreateProjectAsync(
        EcuProjectService service,
        Guid vehicleId,
        Guid originalFileId) =>
        service.CreateAsync(new EcuProjectCreateRequest(
            vehicleId,
            originalFileId,
            "Project",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            null,
            "Unknown",
            null,
            "External read",
            null,
            null));
}
