using SafeEcu.Application.Calibrations;
using SafeEcu.Application.Vehicles;
using SafeEcu.Calibration;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Files;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Calibration;

public sealed class BinaryComparisonServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _backupDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public BinaryComparisonServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "binary-comparison-tests", Guid.NewGuid().ToString("N"));
        _backupDirectory = Path.Combine(_testDirectory, "backups");
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "comparison-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Compare_counts_byte_differences_and_persists_result()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecuInfo = await CreateEcuInfoAsync(vehicle.Id);
        var original = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "original.bin", [0x01, 0x02, 0x03], EcuFileType.Original);
        var modified = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "modified.mod", [0x01, 0xFF, 0x03], EcuFileType.Modified);
        var service = CreateService();

        var result = await service.CompareAsync(new BinaryComparisonRequest(original.Id, modified.Id));
        var comparisons = await service.ListAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.DifferenceCount);
        Assert.Equal(33.333333m, result.Value.PercentChanged);
        Assert.Equal("Different", result.Value.Result);
        Assert.Contains("0x00000001: 0x02 -> 0xFF", result.Value.DifferenceSummary);
        Assert.Contains("0x00000001-0x00000001", result.Value.DifferenceBlockSummary);
        Assert.Single(comparisons);
    }

    [Fact]
    public async Task Compare_persists_changed_offsets_and_blocks()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecuInfo = await CreateEcuInfoAsync(vehicle.Id);
        var original = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "original.bin", [0x10, 0x20, 0x30, 0x40], EcuFileType.Original);
        var modified = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "modified.mod", [0x10, 0x21, 0x31, 0x40], EcuFileType.Modified);
        var service = CreateService();

        await service.CompareAsync(new BinaryComparisonRequest(original.Id, modified.Id));
        var comparisons = await service.ListAsync();
        var comparison = Assert.Single(comparisons);

        Assert.Equal(2, comparison.DifferenceCount);
        Assert.Contains("0x00000001: 0x20 -> 0x21", comparison.DifferenceSummary);
        Assert.Contains("0x00000002: 0x30 -> 0x31", comparison.DifferenceSummary);
        Assert.Contains("0x00000001-0x00000002 (2 differences)", comparison.DifferenceBlockSummary);
    }

    [Fact]
    public async Task Compare_rejects_files_from_different_vehicles()
    {
        await InitializeDatabaseAsync();
        var firstVehicle = await CreateVehicleAsync();
        var secondVehicle = await CreateVehicleAsync("Renault", "Megane 3");
        var firstEcu = await CreateEcuInfoAsync(firstVehicle.Id);
        var secondEcu = await CreateEcuInfoAsync(secondVehicle.Id);
        var original = await ImportFileAsync(firstVehicle.Id, firstEcu.Id, "original.bin", [0x01], EcuFileType.Original);
        var modified = await ImportFileAsync(secondVehicle.Id, secondEcu.Id, "modified.mod", [0x02], EcuFileType.Modified);

        var result = await CreateService().CompareAsync(new BinaryComparisonRequest(original.Id, modified.Id));

        Assert.False(result.IsSuccess);
        Assert.Contains("same vehicle", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compare_requires_modified_file_type()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecuInfo = await CreateEcuInfoAsync(vehicle.Id);
        var original = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "original.bin", [0x01], EcuFileType.Original);
        var wrongType = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "unknown.bin", [0x02], EcuFileType.Unknown);

        var result = await CreateService().CompareAsync(new BinaryComparisonRequest(original.Id, wrongType.Id));

        Assert.False(result.IsSuccess);
        Assert.Contains("Modified", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private BinaryComparisonService CreateService() =>
        new(
            new EcuFileRepository(_dbContextFactory),
            new CalibrationComparisonRepository(_dbContextFactory),
            _logger);

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private async Task<Vehicle> CreateVehicleAsync(string make = "Opel", string model = "Corsa C")
    {
        var vehicle = new Vehicle { Make = make, Model = model };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);
        return vehicle;
    }

    private async Task<EcuInfo> CreateEcuInfoAsync(Guid vehicleId)
    {
        var ecuInfo = new EcuInfo
        {
            VehicleId = vehicleId,
            Manufacturer = "Unknown",
            EcuFamily = "Unknown"
        };
        await new EcuInfoRepository(_dbContextFactory).AddAsync(ecuInfo);
        return ecuInfo;
    }

    private async Task<EcuFile> ImportFileAsync(
        Guid vehicleId,
        Guid ecuInfoId,
        string fileName,
        byte[] bytes,
        EcuFileType fileType)
    {
        var sourceDirectory = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        await File.WriteAllBytesAsync(sourcePath, bytes);

        var ecuFileRepository = new EcuFileRepository(_dbContextFactory);
        var importService = new EcuFileImportService(
            new EcuInfoRepository(_dbContextFactory),
            new EcuFileService(ecuFileRepository, _logger),
            new EcuFileValidationService(ecuFileRepository),
            new Sha256FileHashService(),
            _logger);

        var result = await importService.ImportAsync(new EcuFileImportRequest(
            vehicleId,
            ecuInfoId,
            sourcePath,
            _backupDirectory,
            fileType,
            "External read",
            "ManualWorkflowOnly",
            "FileOnly",
            null));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        return result.Value!;
    }
}
