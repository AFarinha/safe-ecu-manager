using SafeEcu.Application.MapWorkspace;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class MapWorkspacePreviewServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "map-workspace-preview-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task PreviewRawBytes_reads_requested_offsets_for_graphing()
    {
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x10, 0x20, 0x30, 0x40]);
        var service = new MapWorkspacePreviewService();

        var result = await service.PreviewRawBytesAsync(new MapWorkspacePreviewRequest(filePath, StartOffset: 1, Length: 2));

        Assert.True(result.IsSuccess);
        Assert.False(result.EditingAllowed);
        Assert.Equal("raw", result.Unit);
        Assert.Equal([1L, 2L], result.Points.Select(point => point.Offset));
        Assert.Equal([0x20, 0x30], result.Points.Select(point => point.RawByte));
        Assert.Contains(result.BlockReasons, reason => reason.Contains("blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewRawBytes_rejects_unsafe_preview_length()
    {
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x10, 0x20, 0x30, 0x40]);
        var service = new MapWorkspacePreviewService();

        var result = await service.PreviewRawBytesAsync(new MapWorkspacePreviewRequest(filePath, StartOffset: 0, Length: 1024));

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Points);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("512", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewRawBytes_blocks_missing_files()
    {
        var service = new MapWorkspacePreviewService();

        var result = await service.PreviewRawBytesAsync(new MapWorkspacePreviewRequest(
            Path.Combine(_testDirectory, "missing.bin"),
            StartOffset: 0,
            Length: 16));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("not be found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompareRawBytes_returns_original_modified_and_delta_points()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var modifiedPath = Path.Combine(_testDirectory, "modified.bin");
        await File.WriteAllBytesAsync(originalPath, [0x10, 0x20, 0x30, 0x40]);
        await File.WriteAllBytesAsync(modifiedPath, [0x10, 0x22, 0x2F, 0x40]);
        var service = new MapWorkspacePreviewService();

        var result = await service.CompareRawBytesAsync(new MapWorkspaceComparisonRequest(
            originalPath,
            modifiedPath,
            StartOffset: 1,
            Length: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.DifferenceCount);
        Assert.Equal(100m, result.PercentChanged);
        Assert.Equal([1L, 2L], result.Points.Select(point => point.Offset));
        Assert.Equal([0x20, 0x30], result.Points.Select(point => point.OriginalByte));
        Assert.Equal([0x22, 0x2F], result.Points.Select(point => point.ModifiedByte));
        Assert.Equal([2, -1], result.Points.Select(point => point.Difference));
        Assert.Contains(result.BlockReasons, reason => reason.Contains("read-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompareRawBytes_rejects_unsafe_preview_length()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var modifiedPath = Path.Combine(_testDirectory, "modified.bin");
        await File.WriteAllBytesAsync(originalPath, [0x10]);
        await File.WriteAllBytesAsync(modifiedPath, [0x11]);
        var service = new MapWorkspacePreviewService();

        var result = await service.CompareRawBytesAsync(new MapWorkspaceComparisonRequest(
            originalPath,
            modifiedPath,
            StartOffset: 0,
            Length: 1024));

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Points);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("512", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
