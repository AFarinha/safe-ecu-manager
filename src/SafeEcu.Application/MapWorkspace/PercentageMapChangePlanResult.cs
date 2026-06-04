using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed record PercentageMapChangePlanResult(
    bool IsAllowed,
    IReadOnlyList<CalibrationChange> Changes,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
