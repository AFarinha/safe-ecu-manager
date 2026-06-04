namespace SafeEcu.Application.MapWorkspace;

public sealed record DefinedMapComparisonResult(
    bool IsSuccess,
    string MapId,
    IReadOnlyList<DefinedMapComparisonPoint> Points,
    long ChangedCells,
    decimal AverageAbsoluteDelta,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
