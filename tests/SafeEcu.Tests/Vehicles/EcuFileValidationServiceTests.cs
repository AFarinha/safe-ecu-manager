using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Vehicles;

public sealed class EcuFileValidationServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuFileValidationServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "ecu-validation-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "validation-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Valid_file_with_unknown_metadata_returns_warnings_but_is_valid()
    {
        await InitializeDatabaseAsync();
        var sourcePath = await CreateSourceFileAsync("valid.bin", [0x01, 0x02]);
        var service = CreateValidationService();

        var result = await service.ValidateForImportAsync(new EcuFileValidationRequest(
            Guid.NewGuid(),
            EcuInfoId: null,
            sourcePath,
            EcuFileType.Unknown,
            FileOrigin: null,
            ReadMethod: null,
            ProgrammerUsed: null));

        Assert.True(result.IsValid);
        Assert.Equal(EcuFileValidationSeverity.Warning, result.Severity);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task Empty_file_is_invalid()
    {
        await InitializeDatabaseAsync();
        var sourcePath = await CreateSourceFileAsync("empty.bin", []);
        var service = CreateValidationService();

        var result = await service.ValidateForImportAsync(new EcuFileValidationRequest(
            Guid.NewGuid(),
            EcuInfoId: null,
            sourcePath,
            EcuFileType.Original,
            "External read",
            "ManualWorkflowOnly",
            "FileOnly"));

        Assert.False(result.IsValid);
        Assert.Equal(EcuFileValidationSeverity.Error, result.Severity);
        Assert.Contains(result.Errors, error => error.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Duplicate_hash_is_invalid()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var hash = new string('c', 64);
        await new EcuFileRepository(_dbContextFactory).AddAsync(new EcuFile
        {
            VehicleId = vehicle.Id,
            FileName = "existing.bin",
            FilePath = "backups/existing.bin",
            SizeBytes = 1,
            Sha256Hash = hash
        });
        var sourcePath = await CreateSourceFileAsync("new.bin", [0x05]);
        var service = CreateValidationService();

        var result = await service.ValidateForImportAsync(new EcuFileValidationRequest(
            vehicle.Id,
            EcuInfoId: null,
            sourcePath,
            EcuFileType.Original,
            "External read",
            "ManualWorkflowOnly",
            "FileOnly",
            hash));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("same SHA-256", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private EcuFileValidationService CreateValidationService() =>
        new(new EcuFileRepository(_dbContextFactory));

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private async Task<Vehicle> CreateVehicleAsync()
    {
        var vehicle = new Vehicle { Make = "Opel", Model = "Corsa C" };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);
        return vehicle;
    }

    private async Task<string> CreateSourceFileAsync(string fileName, byte[] bytes)
    {
        var sourceDirectory = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        await File.WriteAllBytesAsync(sourcePath, bytes);
        return sourcePath;
    }
}
