using SafeEcu.Application.Safety;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Tests.Safety;

public sealed class SafetyLimitEngineTests
{
    [Fact]
    public void Validate_allows_value_inside_conservative_limit()
    {
        var engine = new SafetyLimitEngine();

        var result = engine.Validate(
            [new SafetyParameterProposal("RailPressure", 1200m, 1300m)],
            [CreateLimit("RailPressure", 0m, 1500m, "bar")]);

        Assert.True(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Safe, result.Status);
        Assert.Empty(result.BlockReasons);
        Assert.Equal(CalibrationSafetyStatus.Safe, result.CheckedParameters.Single().Status);
    }

    [Fact]
    public void Validate_warns_when_value_is_close_to_limit()
    {
        var engine = new SafetyLimitEngine();

        var result = engine.Validate(
            [new SafetyParameterProposal("TurboPressure", 1.1m, 1.46m)],
            [CreateLimit("TurboPressure", 0m, 1.5m, "bar")]);

        Assert.True(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Warning, result.Status);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Validate_blocks_value_above_limit()
    {
        var engine = new SafetyLimitEngine();

        var result = engine.Validate(
            [new SafetyParameterProposal("InjectionQuantity", 45m, 80m)],
            [CreateLimit("InjectionQuantity", 0m, 60m, "mg/stroke")]);

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Blocked, result.Status);
        Assert.Single(result.BlockReasons);
        Assert.Equal(CalibrationSafetyStatus.Blocked, result.CheckedParameters.Single().Status);
    }

    [Fact]
    public void Validate_blocks_unknown_parameter_without_limit()
    {
        var engine = new SafetyLimitEngine();

        var result = engine.Validate(
            [new SafetyParameterProposal("UnknownTorqueLimiter", 100m, 120m)],
            []);

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Unknown, result.Status);
        Assert.Contains("No conservative safety limit", result.BlockReasons.Single());
        Assert.Equal(CalibrationSafetyStatus.Unknown, result.CheckedParameters.Single().Status);
    }

    [Fact]
    public void Validate_blocks_empty_parameter_set()
    {
        var engine = new SafetyLimitEngine();

        var result = engine.Validate([], [CreateLimit("RailPressure", 0m, 1500m, "bar")]);

        Assert.False(result.IsAllowed);
        Assert.Equal(CalibrationSafetyStatus.Blocked, result.Status);
        Assert.Single(result.BlockReasons);
    }

    private static SafetyLimit CreateLimit(
        string parameterName,
        decimal minValue,
        decimal maxValue,
        string unit) =>
        new()
        {
            EcuFamily = "Test ECU",
            SoftwareVersion = "Test",
            EngineCode = "Test",
            ParameterName = parameterName,
            MinValue = minValue,
            MaxValue = maxValue,
            Unit = unit,
            Severity = "Blocked",
            Reason = $"Conservative limit for {parameterName}.",
            SafetyStatus = "Conservative",
            Source = "Unit test"
        };
}
