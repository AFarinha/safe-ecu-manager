using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class SafetyLimitRepository : ISafetyLimitRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public SafetyLimitRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(SafetyLimit safetyLimit, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.SafetyLimits.Add(safetyLimit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SafetyLimit>> ListByProfileAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.SafetyLimits
            .Where(limit => limit.ProfileId == profileId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
