using SafeEcu.Application.Common;
using SafeEcu.Application.Programmers;

namespace SafeEcu.Programmers.Adapters;

public sealed class FileOnlyProgrammerAdapter : IProgrammerAdapter
{
    public ProgrammerCapability GetCapability() =>
        new(
            "File Only Workflow",
            "Local file import",
            ProgrammerSupportedMode.FileOnly,
            DirectReadSupported: false,
            DirectWriteSupported: false,
            RequiresExternalSoftware: false,
            Notes: "Use ECU files read by external tools. This adapter only supports safe file management inside the application.");

    public Task<OperationResult> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Direct ECU read is not supported in FileOnly mode. Import an externally read ECU file instead."));

    public Task<OperationResult> WriteAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Direct ECU write is not supported in FileOnly mode."));
}
