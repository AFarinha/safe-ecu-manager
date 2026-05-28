using SafeEcu.Application.CalibrationProfiles;

namespace SafeEcu.Tests.CalibrationProfiles;

public sealed class ProfilePercentageLimitServiceTests
{
    [Fact]
    public void Validate_allows_diagnostic_only_zero_percent_with_any_ecu()
    {
        var service = CreateService();

        var result = service.Validate(new ProfilePercentageLimitRequest(
            "diagnostic-only",
            "Delco/Delphi/Isuzu Opel 1.7 DTI",
            "Y17DT",
            "Unknown",
            0m));

        Assert.True(result.IsAllowed);
        Assert.Equal(0m, result.MinimumPercentage);
        Assert.Equal(0m, result.MaximumPercentage);
    }

    [Fact]
    public void Validate_blocks_profile_without_specific_limit()
    {
        var service = CreateService();

        var result = service.Validate(new ProfilePercentageLimitRequest(
            "conservative-torque",
            "Delco/Delphi/Isuzu Opel 1.7 DTI",
            "Y17DT",
            "Unknown",
            3m));

        Assert.False(result.IsAllowed);
        Assert.Contains("No ECU/motor/software-specific percentage limit", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_percentage_outside_configured_limit()
    {
        var service = CreateService();

        var result = service.Validate(new ProfilePercentageLimitRequest(
            "diagnostic-only",
            "Any",
            "Any",
            "Any",
            1m));

        Assert.False(result.IsAllowed);
        Assert.Equal(0m, result.MinimumPercentage);
        Assert.Equal(0m, result.MaximumPercentage);
    }

    [Fact]
    public void Validate_blocks_missing_identification_data()
    {
        var service = CreateService();

        var result = service.Validate(new ProfilePercentageLimitRequest(
            "diagnostic-only",
            "",
            "Y17DT",
            "Unknown",
            0m));

        Assert.False(result.IsAllowed);
        Assert.Contains("required", result.BlockReasons.Single());
    }

    private static ProfilePercentageLimitService CreateService() =>
        new(new ProfilePercentageLimitCatalog());
}
