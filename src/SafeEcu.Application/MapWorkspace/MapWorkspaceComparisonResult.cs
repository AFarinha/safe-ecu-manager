namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceComparisonResult(
    bool IsSuccess,
    IReadOnlyList<MapWorkspaceComparisonPoint> Points,
    long DifferenceCount,
    decimal PercentChanged,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
