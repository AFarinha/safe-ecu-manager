namespace SafeEcu.Application.CalibrationProfiles;

public sealed record ProfilePercentageLimitRequest(
    string ProfileKey,
    string EcuFamily,
    string EngineCode,
    string SoftwareVersion,
    decimal RequestedPercentage);
