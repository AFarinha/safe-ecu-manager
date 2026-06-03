using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.PercentageIntent;

public sealed class PercentageIntentEngineTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public PercentageIntentEngineTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "percentage-intent-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "percentage-intent.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Evaluate_allows_diagnostic_only_when_vehicle_and_ecu_are_supported_for_preview()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecu = await CreateEcuAsync(vehicle.Id, SupportStatus.CalibrationPreview);

        var result = await CreateEngine().EvaluateAsync(new PercentageIntentRequest(
            vehicle.Id,
            ecu.Id,
            "diagnostic-only",
            0m));

        Assert.True(result.IsAllowed);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Contains("No ECU data changes", result.ChangeSet.PlannedOperations.Single());
    }

    [Fact]
    public async Task Evaluate_blocks_when_ecu_identification_is_not_high_confidence()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecu = await CreateEcuAsync(vehicle.Id, SupportStatus.CalibrationPreview, highConfidence: false);

        var result = await CreateEngine().EvaluateAsync(new PercentageIntentRequest(
            vehicle.Id,
            ecu.Id,
            "diagnostic-only",
            0m));

        Assert.False(result.IsAllowed);
        Assert.Contains("confidence", result.SafetyReport.BlockReasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Evaluate_blocks_real_percentage_profile_without_technical_rules()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var ecu = await CreateEcuAsync(vehicle.Id, SupportStatus.Verified);

        var result = await CreateEngine().EvaluateAsync(new PercentageIntentRequest(
            vehicle.Id,
            ecu.Id,
            "conservative-torque",
            3m));

        Assert.False(result.IsAllowed);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Contains(
            result.SafetyReport.BlockReasons,
            reason => reason.Contains("Technical map conversion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Evaluate_blocks_renault_megane_percentage_until_verified_map_rules_exist()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync("Renault", "Megane 3");
        var ecu = await CreateEcuAsync(vehicle.Id, SupportStatus.Verified);

        var result = await CreateEngine().EvaluateAsync(new PercentageIntentRequest(
            vehicle.Id,
            ecu.Id,
            "smooth-response",
            2m));

        Assert.False(result.IsAllowed);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Contains(
            result.SafetyReport.BlockReasons,
            reason => reason.Contains("Technical map conversion", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.SafetyReport.BlockReasons,
            reason => reason.Contains("Checksum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Evaluate_blocks_when_ecu_belongs_to_other_vehicle()
    {
        await InitializeDatabaseAsync();
        var vehicle = await CreateVehicleAsync();
        var otherVehicle = await CreateVehicleAsync("Renault", "Megane 3");
        var ecu = await CreateEcuAsync(otherVehicle.Id, SupportStatus.CalibrationPreview);

        var result = await CreateEngine().EvaluateAsync(new PercentageIntentRequest(
            vehicle.Id,
            ecu.Id,
            "diagnostic-only",
            0m));

        Assert.False(result.IsAllowed);
        Assert.Contains("does not belong", result.SafetyReport.BlockReasons.Single());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private PercentageIntentEngine CreateEngine() =>
        new(
            new VehicleRepository(_dbContextFactory),
            new EcuInfoRepository(_dbContextFactory),
            new PercentageIntentValidationService(new CalibrationProfileCatalog()),
            new EcuIdentificationService());

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private async Task<Vehicle> CreateVehicleAsync(string make = "Renault", string model = "Megane 3")
    {
        var vehicle = new Vehicle { Make = make, Model = model };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);
        return vehicle;
    }

    private async Task<EcuInfo> CreateEcuAsync(
        Guid vehicleId,
        SupportStatus supportStatus,
        bool highConfidence = true)
    {
        var ecu = new EcuInfo
        {
            VehicleId = vehicleId,
            Manufacturer = highConfidence ? "Delphi" : "Unknown",
            EcuFamily = highConfidence ? "Delphi DCM" : "Unknown",
            HardwareReference = highConfidence ? "HW-Test" : string.Empty,
            SoftwareReference = highConfidence ? "SW-Test" : string.Empty,
            SoftwareVersion = highConfidence ? "1.0" : string.Empty,
            Protocol = highConfidence ? "ManualWorkflowOnly" : string.Empty,
            SupportStatus = supportStatus
        };

        await new EcuInfoRepository(_dbContextFactory).AddAsync(ecu);
        return ecu;
    }
}
