namespace SafeEcu.Application.PercentageIntent;

public sealed record CalibrationChangeSet(
    string ProfileKey,
    decimal RequestedPercentage,
    IReadOnlyList<string> PlannedOperations,
    bool CreatesModifiedFile);
