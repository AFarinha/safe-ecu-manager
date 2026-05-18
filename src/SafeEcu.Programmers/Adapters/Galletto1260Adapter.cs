using SafeEcu.Application.Common;
using SafeEcu.Application.Programmers;

namespace SafeEcu.Programmers.Adapters;

public sealed class Galletto1260Adapter : IProgrammerAdapter
{
    public ProgrammerCapability GetCapability() =>
        new(
            "Galletto 1260 / EOBD Programmer 1260",
            "USB/OBD",
            ProgrammerSupportedMode.ManualWorkflowOnly,
            DirectReadSupported: false,
            DirectWriteSupported: false,
            RequiresExternalSoftware: true,
            Notes: "Use external Galletto 1260 software to read ECU file, then import the file into this application.");

    public Task<OperationResult> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Direct read from Galletto 1260 is not supported. Use the external Galletto 1260 software and import the resulting file."));

    public Task<OperationResult> WriteAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult.Failure("Direct write with Galletto 1260 is not supported by this application."));
}
