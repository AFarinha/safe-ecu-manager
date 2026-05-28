namespace SafeEcu.Application.CalibrationRules;

public sealed record CalibrationSafetyReport(
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BlockReasons);
