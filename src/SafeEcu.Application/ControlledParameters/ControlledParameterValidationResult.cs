namespace SafeEcu.Application.ControlledParameters;

public sealed record ControlledParameterValidationResult(
    bool IsAllowed,
    ControlledParameterStatus Status,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
