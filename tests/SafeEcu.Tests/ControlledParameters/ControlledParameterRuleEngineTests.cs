using SafeEcu.Application.ControlledParameters;

namespace SafeEcu.Tests.ControlledParameters;

public sealed class ControlledParameterRuleEngineTests
{
    [Fact]
    public void Validate_allows_profile_parameters_when_dependencies_are_present()
    {
        var engine = CreateEngine();

        var result = engine.Validate(new ControlledParameterProposal(
            "conservative-torque",
            ["TorqueLimiter", "FuelQuantity", "SmokeLimiter", "RailPressure"]));

        Assert.True(result.IsAllowed);
        Assert.Equal(ControlledParameterStatus.Allowed, result.Status);
        Assert.Empty(result.BlockReasons);
    }

    [Fact]
    public void Validate_blocks_isolated_parameter_change()
    {
        var engine = CreateEngine();

        var result = engine.Validate(new ControlledParameterProposal(
            "conservative-torque",
            ["FuelQuantity"]));

        Assert.False(result.IsAllowed);
        Assert.Contains("missing required dependencies", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_parameter_not_allowed_for_profile()
    {
        var engine = CreateEngine();

        var result = engine.Validate(new ControlledParameterProposal(
            "eco-conservative",
            ["TurboPressure"]));

        Assert.False(result.IsAllowed);
        Assert.Contains("not allowed", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_profile_without_rules()
    {
        var engine = CreateEngine();

        var result = engine.Validate(new ControlledParameterProposal(
            "stage-1-conservative",
            ["FuelQuantity"]));

        Assert.False(result.IsAllowed);
        Assert.Contains("No controlled parameter rules", result.BlockReasons.Single());
    }

    private static ControlledParameterRuleEngine CreateEngine() =>
        new(new ControlledParameterRuleCatalog());
}
