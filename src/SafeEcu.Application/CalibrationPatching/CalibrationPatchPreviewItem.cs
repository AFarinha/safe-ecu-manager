namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationPatchPreviewItem(
    string MapId,
    string DisplayName,
    string ParameterName,
    long AbsoluteOffset,
    byte CurrentValue,
    byte ProposedValue,
    IReadOnlyList<string> RequiredMapIds,
    string Reason);
