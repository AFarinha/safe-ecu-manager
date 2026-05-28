using Microsoft.Data.Sqlite;
using SafeEcu.Application.Auditing;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Auditing;

public sealed class AuditLogServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public AuditLogServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "audit-log-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "audit-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Record_persists_audit_entry_with_correlation_id()
    {
        await InitializeDatabaseAsync();
        var service = CreateService();
        var correlationId = Guid.NewGuid();

        var result = await service.RecordAsync(new AuditLogRequest(
            "EcuFile.Import",
            "EcuFile",
            "file-1",
            "Information",
            "ECU file imported through manual workflow.",
            "SHA-256 captured.",
            "test-machine",
            correlationId));

        var entries = await service.ListRecentAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(entries);
        Assert.Equal("EcuFile.Import", entries[0].Action);
        Assert.Equal(correlationId, entries[0].CorrelationId);
        Assert.Equal("test-machine", entries[0].UserOrMachineName);
    }

    [Fact]
    public async Task Record_rejects_missing_action()
    {
        await InitializeDatabaseAsync();
        var service = CreateService();

        var result = await service.RecordAsync(new AuditLogRequest(
            "",
            "Vehicle",
            "vehicle-1",
            "Warning",
            "Missing action should fail."));

        Assert.False(result.IsSuccess);
        Assert.Contains("action", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private AuditLogService CreateService() =>
        new(new AuditLogRepository(_dbContextFactory), _logger);

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }
}
