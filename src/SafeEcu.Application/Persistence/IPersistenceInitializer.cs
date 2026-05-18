using SafeEcu.Application.Common;

namespace SafeEcu.Application.Persistence;

public interface IPersistenceInitializer
{
    Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default);
}
