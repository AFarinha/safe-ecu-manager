namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceHexRow(
    long StartOffset,
    IReadOnlyList<string> HexBytes,
    string AsciiPreview);
