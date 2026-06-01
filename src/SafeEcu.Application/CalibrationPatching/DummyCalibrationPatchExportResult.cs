namespace SafeEcu.Application.CalibrationPatching;

public sealed record DummyCalibrationPatchExportResult(
    bool IsAllowed,
    bool CreatesModifiedFile,
    string? OutputFilePath,
    string? OutputSha256Hash,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
