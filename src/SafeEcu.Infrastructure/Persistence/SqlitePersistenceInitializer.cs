using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;

namespace SafeEcu.Infrastructure.Persistence;

public sealed class SqlitePersistenceInitializer : IPersistenceInitializer
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly IAppLogger _logger;

    public SqlitePersistenceInitializer(SafeEcuDbContextFactory dbContextFactory, IAppLogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = _dbContextFactory.Create();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            _logger.Information("SQLite database initialized.");

            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to initialize SQLite database.", exception);
            return OperationResult.Failure("The local database could not be initialized.");
        }
    }
}
