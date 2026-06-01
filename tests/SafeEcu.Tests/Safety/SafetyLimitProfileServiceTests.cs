using Microsoft.Data.Sqlite;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Safety;

public sealed class SafetyLimitProfileServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public SafetyLimitProfileServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "safety-limit-profile-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "safety-limits.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Validate_blocks_when_no_limits_are_persisted_for_profile()
    {
        await InitializeDatabaseAsync();
        var service = CreateService();

        var result = await service.ValidateAsync(new SafetyLimitProfileValidationRequest(
            "dummy-test-profile",
            [new SafetyParameterProposal("FuelQuantity", 20m, 30m)]));

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Unknown, result.Status);
        Assert.Contains("No conservative safety limits", result.BlockReasons.Single());
    }

    [Fact]
    public async Task Validate_uses_persisted_limits_for_profile()
    {
        await InitializeDatabaseAsync();
        await new SafetyLimitRepository(_dbContextFactory).AddAsync(CreateLimit("dummy-test-profile"));

        var result = await CreateService().ValidateAsync(new SafetyLimitProfileValidationRequest(
            "dummy-test-profile",
            [new SafetyParameterProposal("FuelQuantity", 20m, 30m)]));

        Assert.True(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Safe, result.Status);
    }

    [Fact]
    public async Task Validate_blocks_when_persisted_limit_is_exceeded()
    {
        await InitializeDatabaseAsync();
        await new SafetyLimitRepository(_dbContextFactory).AddAsync(CreateLimit("dummy-test-profile"));

        var result = await CreateService().ValidateAsync(new SafetyLimitProfileValidationRequest(
            "dummy-test-profile",
            [new SafetyParameterProposal("FuelQuantity", 20m, 80m)]));

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Blocked, result.Status);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private SafetyLimitProfileService CreateService() =>
        new(new SafetyLimitRepository(_dbContextFactory), new SafetyLimitEngine());

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }

    private static SafetyLimit CreateLimit(string profileId) =>
        new()
        {
            EcuFamily = "Dummy ECU",
            SoftwareVersion = "1.0",
            EngineCode = "DUMMY",
            ProfileId = profileId,
            MapId = "FuelQuantity",
            ParameterName = "FuelQuantity",
            MinValue = 0m,
            MaxValue = 60m,
            Unit = "raw",
            Reason = "Conservative dummy safety limit.",
            Source = "Unit test"
        };
}
