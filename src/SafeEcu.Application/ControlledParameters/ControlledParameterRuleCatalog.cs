namespace SafeEcu.Application.ControlledParameters;

public sealed class ControlledParameterRuleCatalog
{
    private static readonly IReadOnlyList<ControlledParameterRule> Rules =
    [
        new(
            "eco-conservative",
            "FuelQuantity",
            ["SmokeLimiter", "TorqueLimiter"],
            "Fuel changes require smoke and torque limiter dependency checks."),
        new(
            "eco-conservative",
            "DriverRequest",
            ["TorqueLimiter"],
            "Driver request changes require torque limiter dependency checks."),
        new(
            "smooth-response",
            "DriverRequest",
            ["TorqueLimiter", "SmokeLimiter"],
            "Pedal/driver request changes cannot be isolated."),
        new(
            "conservative-torque",
            "TorqueLimiter",
            ["FuelQuantity", "SmokeLimiter", "RailPressure"],
            "Torque limiter changes require fuel, smoke and rail pressure checks."),
        new(
            "conservative-torque",
            "FuelQuantity",
            ["TorqueLimiter", "SmokeLimiter", "RailPressure"],
            "Fuel quantity changes require torque, smoke and rail pressure checks."),
        new(
            "conservative-torque",
            "TurboPressure",
            ["SmokeLimiter", "TorqueLimiter", "FuelQuantity"],
            "Turbo pressure changes require air/fuel/torque dependency checks.")
    ];

    public IReadOnlyList<ControlledParameterRule> ListForProfile(string profileKey) =>
        Rules
            .Where(rule => string.Equals(rule.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase))
            .ToList();
}
