namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceComparisonPoint(
    long Offset,
    byte? OriginalByte,
    byte? ModifiedByte,
    int? Difference);
