using SafeEcu.Application.CalibrationRules;
using SafeEcu.Application.ControlledParameters;
using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Tests.CalibrationRules;

public sealed class CalibrationRuleEngineTests
{
    [Fact]
    public void Evaluate_blocks_when_profile_is_not_supported()
    {
        var result = CreateEngine().Evaluate(new CalibrationRuleRequest(
            "conservative-torque",
            ["TorqueLimiter", "FuelQuantity", "SmokeLimiter", "RailPressure"],
            [new SafetyParameterProposal("FuelQuantity", 40m, 45m)],
            [CreateLimit("FuelQuantity", 0m, 60m)],
            HasOriginalBackup: true,
            IsChecksumSupported: true,
            AreMapsIdentified: true));

        Assert.False(result.IsAllowed);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_blocks_without_original_backup_checksum_or_map_identification()
    {
        var result = CreateEngine().Evaluate(new CalibrationRuleRequest(
            "technical-comparison",
            ["FuelQuantity"],
            [new SafetyParameterProposal("FuelQuantity", 40m, 45m)],
            [CreateLimit("FuelQuantity", 0m, 60m)],
            HasOriginalBackup: false,
            IsChecksumSupported: false,
            AreMapsIdentified: false));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("Original backup"));
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("Checksum"));
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("maps"));
    }

    [Fact]
    public void Evaluate_blocks_when_safety_limit_is_missing()
    {
        var result = CreateEngine().Evaluate(new CalibrationRuleRequest(
            "technical-comparison",
            ["FuelQuantity"],
            [new SafetyParameterProposal("FuelQuantity", 40m, 45m)],
            [],
            HasOriginalBackup: true,
            IsChecksumSupported: true,
            AreMapsIdentified: true));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.SafetyReport.BlockReasons, reason => reason.Contains("No conservative safety limit"));
    }

    private static CalibrationRuleEngine CreateEngine() =>
        new(
            new GuidedProfileService(new GuidedCalibrationProfileCatalog()),
            new ControlledParameterRuleEngine(new ControlledParameterRuleCatalog()),
            new SafetyLimitEngine());

    private static SafetyLimit CreateLimit(string parameterName, decimal minValue, decimal maxValue) =>
        new()
        {
            ParameterName = parameterName,
            MinValue = minValue,
            MaxValue = maxValue,
            Unit = "test",
            Reason = "Conservative test limit."
        };
}
