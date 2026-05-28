using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Checksums;

public sealed class UnsupportedChecksumAlgorithm : IChecksumAlgorithm
{
    public string Name => "Unsupported";

    public bool Supports(EcuInfo ecuInfo) => false;

    public Task<ChecksumValidationResult> ValidateAsync(
        EcuInfo ecuInfo,
        EcuFile ecuFile,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChecksumValidationResult(
            ChecksumValidationStatus.NotSupported,
            Name,
            "No checksum algorithm is registered for this ECU."));
}
