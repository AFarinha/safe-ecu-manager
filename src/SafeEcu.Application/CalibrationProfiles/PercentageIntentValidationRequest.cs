namespace SafeEcu.Application.CalibrationProfiles;

public sealed record PercentageIntentValidationRequest(
    string ProfileKey,
    decimal RequestedPercentage);
