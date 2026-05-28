namespace SafeEcu.Application.CalibrationProfiles;

public sealed record PercentageIntentValidationResult(
    bool IsAllowed,
    CalibrationProfileSupportStatus SupportStatus,
    string ProfileKey,
    decimal RequestedPercentage,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
