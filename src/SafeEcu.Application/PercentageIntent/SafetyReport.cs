namespace SafeEcu.Application.PercentageIntent;

public sealed record SafetyReport(
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
