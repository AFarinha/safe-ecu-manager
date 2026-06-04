using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Projects;

public sealed record EcuProjectAddVersionRequest(
    Guid ProjectId,
    Guid EcuFileId,
    EcuProjectFileVersionKind Kind,
    string Label,
    string? Notes);
