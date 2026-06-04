namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapEditPreviewRequest(
    EcuProjectMapSnapshotResult Snapshot,
    int StartRow,
    int StartColumn,
    int RowCount,
    int ColumnCount,
    EcuProjectMapEditOperation Operation,
    decimal Value,
    decimal? MinimumAllowed,
    decimal? MaximumAllowed);
