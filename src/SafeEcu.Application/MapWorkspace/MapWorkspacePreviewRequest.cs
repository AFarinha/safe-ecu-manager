namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspacePreviewRequest(
    string FilePath,
    long StartOffset,
    int Length,
    decimal Factor = 1m,
    string Unit = "raw");
