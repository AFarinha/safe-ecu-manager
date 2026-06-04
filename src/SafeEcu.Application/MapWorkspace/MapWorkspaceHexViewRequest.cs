namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceHexViewRequest(
    string FilePath,
    long StartOffset,
    int Length,
    int BytesPerRow = 16);
