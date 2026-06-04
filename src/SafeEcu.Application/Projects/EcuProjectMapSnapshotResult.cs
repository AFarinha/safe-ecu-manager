namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapSnapshotResult(
    bool IsSuccess,
    IReadOnlyList<EcuProjectMapSnapshotCell> Cells,
    int RowCount,
    int ColumnCount,
    string Unit,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
