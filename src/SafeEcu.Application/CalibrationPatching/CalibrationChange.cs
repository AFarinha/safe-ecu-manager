namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationChange(
    string MapId,
    long RelativeOffset,
    byte ProposedValue,
    string Reason);
