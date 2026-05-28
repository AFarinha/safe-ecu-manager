namespace SafeEcu.Application.CalibrationProfiles;

public sealed record CalibrationProfileDefinition(
    string Key,
    string DisplayName,
    string Description,
    decimal MinimumPercentage,
    decimal MaximumPercentage,
    CalibrationProfileSupportStatus SupportStatus,
    string SafetyNote);
