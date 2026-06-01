namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationPatchResult(
    bool IsAllowed,
    CalibrationPatchStatus Status,
    CalibrationChangeSet ChangeSet,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons,
    IReadOnlyList<CalibrationPatchPreviewItem>? PreviewItems = null);
