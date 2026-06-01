namespace SafeEcu.Application.CalibrationPatching;

public sealed record OriginalEcuFileValidationResult(
    bool IsValid,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
