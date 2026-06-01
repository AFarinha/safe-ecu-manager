namespace SafeEcu.Application.CalibrationPatching;

public sealed record ValidatedCalibrationChangeSetResult(
    bool IsAllowed,
    CalibrationChangeSet? ChangeSet,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
