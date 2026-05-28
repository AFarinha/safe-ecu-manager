using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.PercentageIntent;

public sealed class PercentageIntentEngineBlockingTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public PercentageIntentEngineBlockingTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "percentage-intent-blocking-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "percentage-intent-blocking.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Evaluate_blocks_when_vehicle_and_ecu_do_not_exist()
    {
        await InitializeDatabaseAsync();
        var engine = CreateEngine();

        var result = await engine.EvaluateAsync(new PercentageIntentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "diagnostic-only",
            0m));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("Vehicle was not found"));
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("ECU/EDU record was not found"));
        Assert.False(result.ChangeSet.CreatesModifiedFile);
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
}
