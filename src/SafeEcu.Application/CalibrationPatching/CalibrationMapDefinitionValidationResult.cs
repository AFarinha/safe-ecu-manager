namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapDefinitionValidationResult(
    bool IsAllowed,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
