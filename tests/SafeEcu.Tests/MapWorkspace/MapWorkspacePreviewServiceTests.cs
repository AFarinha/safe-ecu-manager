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

    [Fact]
    public async Task PreviewHex_returns_offsets_hex_bytes_and_ascii_preview()
    {
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x41, 0x42, 0x00, 0x7F, 0x43]);
        var service = new MapWorkspacePreviewService();

        var result = await service.PreviewHexAsync(new MapWorkspaceHexViewRequest(
            filePath,
            StartOffset: 1,
            Length: 4,
            BytesPerRow: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, result.Rows[0].StartOffset);
        Assert.Equal(["42", "00"], result.Rows[0].HexBytes);
        Assert.Equal("B.", result.Rows[0].AsciiPreview);
        Assert.Equal(3, result.Rows[1].StartOffset);
        Assert.Equal(["7F", "43"], result.Rows[1].HexBytes);
        Assert.Equal(".C", result.Rows[1].AsciiPreview);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("read-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchBytePattern_returns_matching_offsets()
    {
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x10, 0x20, 0x30, 0x10, 0x20, 0x40]);
        var service = new MapWorkspacePreviewService();

        var result = await service.SearchBytePatternAsync(new MapWorkspaceBytePatternSearchRequest(
            filePath,
            StartOffset: 0,
            Length: 6,
            HexPattern: "10 20"));

        Assert.True(result.IsSuccess);
        Assert.Equal([0L, 3L], result.Matches.Select(match => match.Offset));
        Assert.Contains(result.Messages, message => message.Contains("2 match", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.BlockReasons, reason => reason.Contains("manually verified", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchBytePattern_rejects_invalid_hex_pattern()
    {
        Directory.CreateDirectory(_testDirectory);
        var filePath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(filePath, [0x10, 0x20]);
        var service = new MapWorkspacePreviewService();

        var result = await service.SearchBytePatternAsync(new MapWorkspaceBytePatternSearchRequest(
            filePath,
            StartOffset: 0,
            Length: 2,
            HexPattern: "10 XYZ"));

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Matches);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("two digit", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
