using SafeEcu.Application.Projects;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectMapEditPreviewServiceTests
{
    [Fact]
    public void Preview_applies_percentage_to_selected_cells_without_writing_file()
    {
        var service = new EcuProjectMapEditPreviewService();
        var snapshot = CreateSnapshot();

        var result = service.Preview(new EcuProjectMapEditPreviewRequest(
            snapshot,
            StartRow: 0,
            StartColumn: 1,
            RowCount: 2,
            ColumnCount: 1,
            EcuProjectMapEditOperation.ApplyPercentage,
            Value: 10m,
            MinimumAllowed: 0m,
            MaximumAllowed: 100m));

        Assert.True(result.IsAllowed);
        Assert.Equal(2, result.Cells.Count);
        Assert.Equal([22m, 44m], result.Cells.Select(cell => cell.ProposedValue));
        Assert.Contains(result.Messages, message => message.Contains("No file bytes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preview_blocks_values_outside_limits()
    {
        var service = new EcuProjectMapEditPreviewService();
        var snapshot = CreateSnapshot();

        var result = service.Preview(new EcuProjectMapEditPreviewRequest(
            snapshot,
            StartRow: 0,
            StartColumn: 0,
            RowCount: 1,
            ColumnCount: 1,
            EcuProjectMapEditOperation.ApplyMultiplier,
            Value: 10m,
            MinimumAllowed: 0m,
            MaximumAllowed: 50m));

        Assert.False(result.IsAllowed);
        Assert.Single(result.Cells);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("maximum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preview_blocks_invalid_snapshot()
    {
        var service = new EcuProjectMapEditPreviewService();
        var invalidSnapshot = new EcuProjectMapSnapshotResult(false, [], 0, 0, "raw", [], ["Snapshot failed."]);

        var result = service.Preview(new EcuProjectMapEditPreviewRequest(
            invalidSnapshot,
            StartRow: 0,
            StartColumn: 0,
            RowCount: 1,
            ColumnCount: 1,
            EcuProjectMapEditOperation.IncrementAbsolute,
            Value: 1m,
            MinimumAllowed: null,
            MaximumAllowed: null));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("valid read-only", StringComparison.OrdinalIgnoreCase));
    }

    private static EcuProjectMapSnapshotResult CreateSnapshot() =>
        new(
            true,
            [
                new EcuProjectMapSnapshotCell(0, 0, 0, 10, 10m),
                new EcuProjectMapSnapshotCell(0, 1, 1, 20, 20m),
                new EcuProjectMapSnapshotCell(1, 0, 2, 30, 30m),
                new EcuProjectMapSnapshotCell(1, 1, 3, 40, 40m)
            ],
            2,
            2,
            "raw",
            ["Snapshot ready."],
            ["Read-only."]);
}
