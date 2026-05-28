using SafeEcu.Application.CalibrationProfiles;

namespace SafeEcu.Tests.CalibrationProfiles;

public sealed class PercentageIntentValidationServiceTests
{
    [Fact]
    public void Validate_allows_diagnostic_only_zero_percent()
    {
        var service = CreateService();

        var result = service.Validate(new PercentageIntentValidationRequest("diagnostic-only", 0m));

        Assert.True(result.IsAllowed);
        Assert.Equal(CalibrationProfileSupportStatus.Supported, result.SupportStatus);
        Assert.Empty(result.BlockReasons);
    }

    [Fact]
    public void Validate_blocks_unknown_profile()
    {
        var service = CreateService();

        var result = service.Validate(new PercentageIntentValidationRequest("unknown", 1m));

        Assert.False(result.IsAllowed);
        Assert.Contains("unknown", result.BlockReasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_blocks_profile_that_is_not_supported()
    {
        var service = CreateService();

        var result = service.Validate(new PercentageIntentValidationRequest("conservative-torque", 3m));

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationProfileSupportStatus.NotSupported, result.SupportStatus);
        Assert.Contains("cannot be applied", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_percentage_outside_profile_limits()
    {
        var service = CreateService();

        var result = service.Validate(new PercentageIntentValidationRequest("diagnostic-only", 1m));

        Assert.False(result.IsAllowed);
        Assert.Contains("between 0% and 0%", result.BlockReasons.Single());
    }

    [Fact]
    public void Catalog_does_not_mark_power_profiles_as_supported()
    {
        var profiles = new CalibrationProfileCatalog().ListAll();

        Assert.DoesNotContain(
            profiles,
            profile => profile.Key != "diagnostic-only"
                && profile.SupportStatus is CalibrationProfileSupportStatus.Supported
                    or CalibrationProfileSupportStatus.Verified);
    }

    private static PercentageIntentValidationService CreateService() =>
        new(new CalibrationProfileCatalog());
}
