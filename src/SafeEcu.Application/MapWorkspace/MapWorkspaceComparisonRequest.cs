namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceComparisonRequest(
    string OriginalFilePath,
    string ModifiedFilePath,
    long StartOffset,
    int Length);
