using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Auditing;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public AuditLogRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.AuditLogEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        var entries = await dbContext.AuditLogEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries
            .OrderByDescending(entry => entry.Timestamp)
            .Take(Math.Clamp(take, 1, 1000))
            .ToList();
    }
}
