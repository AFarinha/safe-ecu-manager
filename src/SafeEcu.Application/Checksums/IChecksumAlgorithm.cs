using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Checksums;

public interface IChecksumAlgorithm
{
    string Name { get; }

    bool Supports(EcuInfo ecuInfo);

    Task<ChecksumValidationResult> ValidateAsync(
        EcuInfo ecuInfo,
        EcuFile ecuFile,
        CancellationToken cancellationToken = default);
}
