using SafeEcu.Application.Checksums;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Tests.Checksums;

public sealed class ChecksumValidationServiceTests
{
    [Fact]
    public async Task Validate_returns_not_supported_when_no_algorithm_is_registered()
    {
        var service = new ChecksumValidationService(new ChecksumAlgorithmRegistry([]));

        var result = await service.ValidateAsync(
            new EcuInfo { Manufacturer = "Bosch", EcuFamily = "EDC15" },
            new EcuFile { FileName = "original.bin" });

        Assert.Equal(ChecksumValidationStatus.NotSupported, result.Status);
        Assert.False(result.IsSupported);
        Assert.Equal("None", result.AlgorithmName);
    }

    [Fact]
    public void Unsupported_algorithm_never_claims_support()
    {
        var algorithm = new UnsupportedChecksumAlgorithm();

        Assert.False(algorithm.Supports(new EcuInfo { EcuFamily = "EDC15" }));
    }
}
