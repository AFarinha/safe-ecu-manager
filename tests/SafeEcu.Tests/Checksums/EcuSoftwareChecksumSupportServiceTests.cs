using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Tests.Checksums;

public sealed class EcuSoftwareChecksumSupportServiceTests
{
    [Fact]
    public void Evaluate_blocks_export_when_no_algorithm_is_registered()
    {
        var service = new EcuSoftwareChecksumSupportService(new EcuSoftwareChecksumRegistry([]));

        var result = service.Evaluate(CreateProfile());

        Assert.False(result.IsSupported);
        Assert.False(result.CanExportCalibrationOutput);
        Assert.Equal(ChecksumValidationStatus.NotSupported, result.Status);
        Assert.Contains("not registered", result.BlockReasons.Single());
    }

    [Fact]
    public void Evaluate_blocks_export_when_profile_has_no_algorithm_id()
    {
        var service = new EcuSoftwareChecksumSupportService(new EcuSoftwareChecksumRegistry([]));

        var result = service.Evaluate(CreateProfile(checksumAlgorithmId: ""));

        Assert.False(result.CanExportCalibrationOutput);
        Assert.Equal("None", result.AlgorithmId);
    }

    [Fact]
    public void Evaluate_allows_export_only_when_matching_dummy_algorithm_is_registered()
    {
        var service = new EcuSoftwareChecksumSupportService(new EcuSoftwareChecksumRegistry(
            [new DummyChecksumAlgorithm()]));

        var result = service.Evaluate(CreateProfile());

        Assert.True(result.IsSupported);
        Assert.True(result.CanExportCalibrationOutput);
        Assert.Equal(ChecksumValidationStatus.Valid, result.Status);
    }

    private static VerifiedEcuSoftwareProfile CreateProfile(string checksumAlgorithmId = "dummy-checksum") =>
        new(
            "dummy-test-profile",
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            4,
            new string('a', 64),
            ["FuelQuantity"],
            checksumAlgorithmId,
            VerifiedEcuSoftwareSupportStatus.Verified,
            "Unit test",
            "Dummy-only profile.");

    private sealed class DummyChecksumAlgorithm : IEcuSoftwareChecksumAlgorithm
    {
        public string AlgorithmId => "dummy-checksum";

        public bool Supports(VerifiedEcuSoftwareProfile profile) =>
            profile.ProfileId == "dummy-test-profile";
    }
}
