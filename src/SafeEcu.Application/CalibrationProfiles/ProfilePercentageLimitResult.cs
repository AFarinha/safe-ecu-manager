namespace SafeEcu.Application.CalibrationProfiles;

public sealed record ProfilePercentageLimitResult(
    bool IsAllowed,
    decimal RequestedPercentage,
    decimal? MinimumPercentage,
    decimal? MaximumPercentage,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
