namespace SafeEcu.Application.MapWorkspace;

public sealed record DefinedMapComparisonPoint(
    int CellIndex,
    long Offset,
    decimal OriginalValue,
    decimal ModifiedValue,
    decimal Delta,
    decimal PercentDelta);
