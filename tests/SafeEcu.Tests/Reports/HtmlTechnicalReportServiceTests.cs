using SafeEcu.Application.Calibrations;
using SafeEcu.Application.Reports;
using SafeEcu.Application.Vehicles;
using SafeEcu.Calibration;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Files;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Infrastructure.Reports;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Reports;

public sealed class HtmlTechnicalReportServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _backupDirectory;
    private readonly string _reportsDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public HtmlTechnicalReportServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "report-tests", Guid.NewGuid().ToString("N"));
        _backupDirectory = Path.Combine(_testDirectory, "backups");
        _reportsDirectory = Path.Combine(_testDirectory, "reports");
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "report-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task GenerateTechnicalReport_creates_html_file_with_required_warning()
    {
        await InitializeDatabaseAsync();
        var comparisonId = await CreateComparisonAsync();
        var service = new HtmlTechnicalReportService(_dbContextFactory, _logger);

        var result = await service.GenerateTechnicalReportAsync(
            new TechnicalReportRequest(comparisonId, _reportsDirectory));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.Value));

        var html = await File.ReadAllTextAsync(result.Value!);
        Assert.Contains("Safe ECU Technical Report", html);
        Assert.Contains("Checksum valid does not mean calibration safe", html);
        Assert.Contains("SHA-256", html);
        Assert.Contains("Binary comparison", html);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

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

    private async Task<Guid> CreateComparisonAsync()
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

        var ecuInfo = new EcuInfo
        {
            VehicleId = vehicle.Id,
            Manufacturer = "Unknown",
            EcuFamily = "Unknown"
        };
        await new EcuInfoRepository(_dbContextFactory).AddAsync(ecuInfo);

        var original = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "original.bin", [0x01, 0x02], EcuFileType.Original);
        var modified = await ImportFileAsync(vehicle.Id, ecuInfo.Id, "modified.mod", [0x01, 0x03], EcuFileType.Modified);
        var comparisonService = new BinaryComparisonService(
            new EcuFileRepository(_dbContextFactory),
            new CalibrationComparisonRepository(_dbContextFactory),
            _logger);

        var comparison = await comparisonService.CompareAsync(new BinaryComparisonRequest(original.Id, modified.Id));
        Assert.True(comparison.IsSuccess, comparison.ErrorMessage);

        return comparison.Value!.Id;
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
