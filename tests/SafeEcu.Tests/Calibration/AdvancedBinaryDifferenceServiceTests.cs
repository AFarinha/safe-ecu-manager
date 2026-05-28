using SafeEcu.Calibration;

namespace SafeEcu.Tests.Calibration;

public sealed class AdvancedBinaryDifferenceServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "advanced-binary-difference-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Scan_groups_contiguous_differences_and_samples_offsets()
    {
        var original = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04, 0x05]);
        var modified = await WriteFileAsync("modified.bin", [0x01, 0xFF, 0xFE, 0x04, 0xAA]);
        var service = new AdvancedBinaryDifferenceService();

        var result = await service.ScanAsync(original, modified);

        Assert.Equal(3, result.DifferenceCount);
        Assert.Equal(60m, result.PercentChanged);
        Assert.Equal([1L, 2L, 4L], result.SampledDifferences.Select(difference => difference.Offset));
        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal(new BinaryDifferenceBlock(1, 2, 2), result.Blocks[0]);
        Assert.Equal(new BinaryDifferenceBlock(4, 4, 1), result.Blocks[1]);
    }

    [Fact]
    public async Task Scan_marks_missing_bytes_when_file_sizes_differ()
    {
        var original = await WriteFileAsync("original.bin", [0x01, 0x02]);
        var modified = await WriteFileAsync("modified.bin", [0x01, 0x02, 0x03]);
        var service = new AdvancedBinaryDifferenceService();

        var result = await service.ScanAsync(original, modified);

        Assert.Equal(1, result.DifferenceCount);
        Assert.Null(result.SampledDifferences.Single().OriginalByte);
        Assert.Equal((byte?)0x03, result.SampledDifferences.Single().ModifiedByte);
    }

    [Fact]
    public async Task Scan_truncates_samples_and_blocks_when_limits_are_reached()
    {
        var original = await WriteFileAsync("original.bin", [0x00, 0x00, 0x00, 0x00, 0x00]);
        var modified = await WriteFileAsync("modified.bin", [0x01, 0x00, 0x02, 0x00, 0x03]);
        var service = new AdvancedBinaryDifferenceService();

        var result = await service.ScanAsync(
            original,
            modified,
            new BinaryDifferenceScanOptions(MaxSampledDifferences: 2, MaxBlocks: 2));

        Assert.Equal(3, result.DifferenceCount);
        Assert.Equal(2, result.SampledDifferences.Count);
        Assert.True(result.IsSampleTruncated);
        Assert.Equal(2, result.Blocks.Count);
        Assert.True(result.AreBlocksTruncated);
    }

    [Fact]
    public async Task ToCsv_exports_sampled_offsets_only()
    {
        var original = await WriteFileAsync("original.bin", [0x01, 0x02]);
        var modified = await WriteFileAsync("modified.bin", [0x01, 0xFF]);
        var service = new AdvancedBinaryDifferenceService();

        var result = await service.ScanAsync(original, modified);
        var csv = result.ToCsv();

        Assert.Contains("Offset,OriginalByte,ModifiedByte", csv);
        Assert.Contains("1,0x02,0xFF", csv);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private async Task<string> WriteFileAsync(string fileName, byte[] bytes)
    {
        Directory.CreateDirectory(_testDirectory);
        var path = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }
}
