using SafeEcu.Application.Common;

namespace SafeEcu.Application.Programmers;

public interface IProgrammerAdapter
{
    ProgrammerCapability GetCapability();

    Task<OperationResult> ReadAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> WriteAsync(CancellationToken cancellationToken = default);
}
