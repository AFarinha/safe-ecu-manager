namespace SafeEcu.Application.CalibrationProfiles;

public sealed record ProfilePercentageLimit(
    string ProfileKey,
    string EcuFamily,
    string EngineCode,
    string SoftwareVersion,
    decimal MinimumPercentage,
    decimal MaximumPercentage,
    string Source,
    string Notes);
