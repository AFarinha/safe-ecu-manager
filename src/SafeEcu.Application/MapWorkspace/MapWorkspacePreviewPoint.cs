namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspacePreviewPoint(
    long Offset,
    byte RawByte,
    decimal ScaledValue);
