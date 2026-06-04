namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapSnapshotRequest(
    Guid ProjectId,
    string MapId,
    string FilePath);
