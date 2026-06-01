namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapValue(
    string MapId,
    long Offset,
    byte CurrentValue,
    byte ProposedValue);
