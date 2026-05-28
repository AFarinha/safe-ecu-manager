namespace SafeEcu.Application.Safety;

public sealed record CalibrationValidationResult(
    bool IsAllowed,
    CalibrationSafetyStatus Status,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> BlockReasons,
    IReadOnlyList<CalibrationParameterCheck> CheckedParameters);
