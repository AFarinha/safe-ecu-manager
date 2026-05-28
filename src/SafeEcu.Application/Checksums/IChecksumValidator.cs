using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Checksums;

public interface IChecksumValidator
{
    Task<ChecksumValidationResult> ValidateAsync(
        EcuInfo ecuInfo,
        EcuFile ecuFile,
        CancellationToken cancellationToken = default);
}
