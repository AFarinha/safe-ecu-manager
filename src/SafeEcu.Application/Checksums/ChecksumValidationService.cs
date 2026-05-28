using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Checksums;

public sealed class ChecksumValidationService : IChecksumValidator
{
    private readonly ChecksumAlgorithmRegistry _registry;

    public ChecksumValidationService(ChecksumAlgorithmRegistry registry)
    {
        _registry = registry;
    }

    public Task<ChecksumValidationResult> ValidateAsync(
        EcuInfo ecuInfo,
        EcuFile ecuFile,
        CancellationToken cancellationToken = default)
    {
        var algorithm = _registry.FindFor(ecuInfo);
        if (algorithm is null)
        {
            return Task.FromResult(new ChecksumValidationResult(
                ChecksumValidationStatus.NotSupported,
                "None",
                "Checksum validation is not supported for this ECU/family/software version."));
        }

        return algorithm.ValidateAsync(ecuInfo, ecuFile, cancellationToken);
    }
}
