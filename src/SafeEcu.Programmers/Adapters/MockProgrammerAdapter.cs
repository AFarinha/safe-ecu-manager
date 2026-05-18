using SafeEcu.Application.Common;
using SafeEcu.Application.Programmers;

namespace SafeEcu.Programmers.Adapters;

public sealed class MockProgrammerAdapter : IProgrammerAdapter
{
    public ProgrammerCapability GetCapability() =>
        new(
            "Mock Programmer",
            "Test double",
            ProgrammerSupportedMode.Mock,
            DirectReadSupported: false,
            DirectWriteSupported: false,
            RequiresExternalSoftware: false,
            Notes: "Test-only adapter for development workflows. It never communicates with vehicle hardware.");

    public Task<OperationResult> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Mock programmer does not perform real ECU reads."));

    public Task<OperationResult> WriteAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Mock programmer does not perform real ECU writes."));
}
