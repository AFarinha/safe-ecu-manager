namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationChangeSet(
    string ProfileKey,
    CalibrationPatchMode Mode,
    IReadOnlyList<CalibrationMapValue> Values,
    bool CreatesModifiedFile);
