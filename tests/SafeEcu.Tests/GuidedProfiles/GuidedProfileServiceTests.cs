using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.GuidedProfiles;

namespace SafeEcu.Tests.GuidedProfiles;

public sealed class GuidedProfileServiceTests
{
    [Fact]
    public void ListProfiles_includes_expected_safe_workflows()
    {
        var profiles = new GuidedCalibrationProfileCatalog().ListAll();

        Assert.Contains(profiles, profile => profile.Key == "diagnostic-only");
        Assert.Contains(profiles, profile => profile.Key == "technical-comparison");
        Assert.Contains(profiles, profile => profile.Key == "conservative-torque");
    }

    [Fact]
    public void EvaluateAvailability_allows_supported_non_writing_profile()
    {
        var service = CreateService();

        var availability = service.EvaluateAvailability("technical-comparison");

        Assert.True(availability.IsAvailable);
        Assert.Empty(availability.BlockReasons);
    }

    [Fact]
    public void EvaluateAvailability_blocks_not_supported_profile()
    {
        var service = CreateService();

        var availability = service.EvaluateAvailability("conservative-torque");

        Assert.False(availability.IsAvailable);
        Assert.Contains("blocked", availability.BlockReasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_does_not_mark_real_calibration_profiles_as_supported()
    {
        var profiles = new GuidedCalibrationProfileCatalog().ListAll();
        var realCalibrationProfiles = profiles.Where(profile =>
            profile.Key is "eco-conservative" or "smooth-response" or "conservative-torque" or "stage-1-conservative");

        Assert.All(
            realCalibrationProfiles,
            profile => Assert.Equal(CalibrationProfileSupportStatus.NotSupported, profile.SupportStatus));
    }

    [Fact]
    public void Profile_describes_emissions_and_writing_restrictions()
    {
        var profile = new GuidedCalibrationProfileCatalog().FindByKey("eco-conservative");

        Assert.NotNull(profile);
        Assert.Contains(profile.DoesNotDo, item => item.Contains("emissions", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(profile.Requirements, item => item.Contains("Checksum", StringComparison.OrdinalIgnoreCase));
    }

    private static GuidedProfileService CreateService() =>
        new(new GuidedCalibrationProfileCatalog());
}
