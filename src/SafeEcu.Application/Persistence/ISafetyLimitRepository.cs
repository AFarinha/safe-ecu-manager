using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Application.Persistence;

public interface ISafetyLimitRepository
{
    Task AddAsync(SafetyLimit safetyLimit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SafetyLimit>> ListByProfileAsync(
        string profileId,
        CancellationToken cancellationToken = default);
}
