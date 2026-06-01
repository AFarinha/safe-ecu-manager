using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Tests.EcuProfiles;

public sealed class VerifiedEcuSoftwareProfileServiceTests
{
    [Fact]
    public void Catalog_starts_empty_without_real_verified_profiles()
    {
        var profiles = new VerifiedEcuSoftwareProfileCatalog().Profiles;

        Assert.Empty(profiles);
    }

    [Fact]
    public void Match_blocks_when_required_evidence_is_missing()
    {
        var service = CreateService();

        var result = service.Match(new VerifiedEcuSoftwareProfileMatchRequest(
            "",
            "",
            "",
            "",
            0,
            ""));

        Assert.False(result.IsMatch);
        Assert.False(result.IsVerified);
        Assert.Contains("Missing required", result.BlockReasons.Single());
    }

    [Fact]
    public void Match_blocks_when_no_verified_profile_matches()
    {
        var service = CreateService();

        var result = service.Match(CreateCompleteRequest());

        Assert.False(result.IsMatch);
        Assert.False(result.IsVerified);
        Assert.Contains("No verified ECU/software profile", result.BlockReasons.Single());
    }

    [Fact]
    public void Profile_model_carries_required_verified_support_metadata()
    {
        var profile = new VerifiedEcuSoftwareProfile(
            "dummy-test-profile",
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            4,
            new string('a', 64),
            ["FuelQuantity"],
            "dummy-checksum",
            VerifiedEcuSoftwareSupportStatus.Verified,
            "Unit test",
            "Dummy-only profile.");

        Assert.Equal(VerifiedEcuSoftwareSupportStatus.Verified, profile.SupportStatus);
        Assert.Equal("dummy-checksum", profile.ChecksumAlgorithmId);
        Assert.Contains("FuelQuantity", profile.AllowedMapIds);
    }

    private static VerifiedEcuSoftwareProfileService CreateService() =>
        new(new VerifiedEcuSoftwareProfileCatalog());

    private static VerifiedEcuSoftwareProfileMatchRequest CreateCompleteRequest() =>
        new(
            "Delco/Delphi/Isuzu Opel 1.7 DTI",
            "HW-UNKNOWN",
            "SW-UNKNOWN",
            "1.0",
            1024,
            new string('a', 64));
}
