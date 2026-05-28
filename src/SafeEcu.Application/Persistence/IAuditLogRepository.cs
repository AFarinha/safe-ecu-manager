using SafeEcu.Domain.Auditing;

namespace SafeEcu.Application.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default);
}
