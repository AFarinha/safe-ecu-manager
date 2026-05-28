namespace SafeEcu.Application.CalibrationProfiles;

public sealed class ProfilePercentageLimitCatalog
{
    private static readonly IReadOnlyList<ProfilePercentageLimit> Limits =
    [
        new(
            "diagnostic-only",
            "Any",
            "Any",
            "Any",
            0m,
            0m,
            "Built-in safe workflow",
            "Diagnostic-only workflows do not request calibration changes.")
    ];

    public ProfilePercentageLimit? FindSpecific(ProfilePercentageLimitRequest request) =>
        Limits.SingleOrDefault(limit =>
            Matches(limit.ProfileKey, request.ProfileKey)
            && Matches(limit.EcuFamily, request.EcuFamily)
            && Matches(limit.EngineCode, request.EngineCode)
            && Matches(limit.SoftwareVersion, request.SoftwareVersion));

    private static bool Matches(string limitValue, string requestValue) =>
        string.Equals(limitValue, "Any", StringComparison.OrdinalIgnoreCase)
        || string.Equals(limitValue, requestValue, StringComparison.OrdinalIgnoreCase);
}
