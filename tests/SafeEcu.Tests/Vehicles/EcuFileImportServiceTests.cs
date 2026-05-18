using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Files;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Vehicles;

public sealed class EcuFileImportServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _backupDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuFileImportServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "ecu-import-tests", Guid.NewGuid().ToString("N"));
        _backupDirectory = Path.Combine(_testDirectory, "backups");
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "import-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Import_copies_file_registers_metadata_and_creates_unknown_ecu()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var sourcePath = await CreateSourceFileAsync("original.bin", [0x10, 0x20, 0x30, 0x40]);
        var importService = CreateImportService();

        var result = await importService.ImportAsync(new EcuFileImportRequest(
            vehicle.Id,
            EcuInfoId: null,
            sourcePath,
            _backupDirectory,
            EcuFileType.Original,
            "External read",
            "ManualWorkflowOnly",
            "Galletto 1260 / EOBD Programmer 1260",
            "Test import"));

        var ecuInfos = await new EcuInfoRepository(_dbContextFactory).ListByVehicleAsync(vehicle.Id);
        var ecuFiles = await new EcuFileRepository(_dbContextFactory).ListByVehicleAsync(vehicle.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(ecuInfos);
        Assert.Single(ecuFiles);
        Assert.Equal("Unknown", ecuInfos[0].Manufacturer);
        Assert.Equal(ecuInfos[0].Id, ecuFiles[0].EcuInfoId);
        Assert.True(File.Exists(ecuFiles[0].FilePath));
        Assert.Equal(4, ecuFiles[0].SizeBytes);
        Assert.Equal(64, ecuFiles[0].Sha256Hash.Length);
    }

    [Fact]
    public async Task Import_rejects_duplicate_hash()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var sourcePath = await CreateSourceFileAsync("duplicate.bin", [0xAA, 0xBB]);
        var importService = CreateImportService();
        var request = new EcuFileImportRequest(
            vehicle.Id,
            EcuInfoId: null,
            sourcePath,
            _backupDirectory,
            EcuFileType.Original,
            "External read",
            "ManualWorkflowOnly",
            "FileOnly",
            null);

        var first = await importService.ImportAsync(request);
        var duplicate = await importService.ImportAsync(request);

        Assert.True(first.IsSuccess);
        Assert.False(duplicate.IsSuccess);
    }

    [Fact]
    public async Task Import_rejects_unsupported_extension()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var sourcePath = await CreateSourceFileAsync("unsafe.txt", [0x01]);

        var result = await CreateImportService().ImportAsync(new EcuFileImportRequest(
            vehicle.Id,
            EcuInfoId: null,
            sourcePath,
            _backupDirectory,
            EcuFileType.Unknown,
            null,
            null,
            null,
            null));

        Assert.False(result.IsSuccess);
        Assert.Contains("extension", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private EcuFileImportService CreateImportService()
    {
        var ecuInfoRepository = new EcuInfoRepository(_dbContextFactory);
        var ecuFileRepository = new EcuFileRepository(_dbContextFactory);
        var ecuFileService = new EcuFileService(ecuFileRepository, _logger);

        return new EcuFileImportService(
            ecuInfoRepository,
            ecuFileService,
            new Sha256FileHashService(),
            _logger);
    }

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private async Task<Vehicle> CreateVehicleAsync()
    {
        var vehicle = new Vehicle
        {
            Make = "Opel",
            Model = "Corsa C",
            Engine = "1.7 DTI",
            EngineCode = "Y17DT",
            FuelType = FuelType.Diesel
        };

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
