using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.MapWorkspace;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class DefinedMapComparisonServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "defined-map-comparison-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CompareAsync_compares_defined_map_cells()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var modifiedPath = Path.Combine(_testDirectory, "modified.bin");
        await File.WriteAllBytesAsync(originalPath, [0x00, 0x10, 0x20, 0x30, 0x00]);
        await File.WriteAllBytesAsync(modifiedPath, [0x00, 0x10, 0x22, 0x2F, 0x00]);
        var service = new DefinedMapComparisonService();

        var result = await service.CompareAsync(new DefinedMapComparisonRequest(
            originalPath,
            modifiedPath,
            CreateMap(startOffset: 1, length: 3)));

        Assert.True(result.IsSuccess);
        Assert.Equal("FuelQuantity", result.MapId);
        Assert.Equal(2, result.ChangedCells);
        Assert.Equal(1m, result.AverageAbsoluteDelta);
        Assert.Equal([0m, 2m, -1m], result.Points.Select(point => point.Delta));
    }

    [Fact]
    public async Task CompareAsync_blocks_emissions_related_maps()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var modifiedPath = Path.Combine(_testDirectory, "modified.bin");
        await File.WriteAllBytesAsync(originalPath, [0x00, 0x10]);
        await File.WriteAllBytesAsync(modifiedPath, [0x00, 0x11]);
        var service = new DefinedMapComparisonService();

        var result = await service.CompareAsync(new DefinedMapComparisonRequest(
            originalPath,
            modifiedPath,
            CreateMap(isEmissionsRelated: true)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("emissions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CompareAsync_blocks_unverified_maps()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var modifiedPath = Path.Combine(_testDirectory, "modified.bin");
        await File.WriteAllBytesAsync(originalPath, [0x00, 0x10]);
        await File.WriteAllBytesAsync(modifiedPath, [0x00, 0x11]);
        var service = new DefinedMapComparisonService();

        var result = await service.CompareAsync(new DefinedMapComparisonRequest(
            originalPath,
            modifiedPath,
            CreateMap(supportStatus: CalibrationMapDefinitionSupportStatus.Experimental)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("not verified", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static CalibrationMapDefinition CreateMap(
        long startOffset = 0,
        int length = 2,
        bool isEmissionsRelated = false,
        CalibrationMapDefinitionSupportStatus supportStatus = CalibrationMapDefinitionSupportStatus.Verified) =>
        new(
            "FuelQuantity",
            "Fuel quantity",
            "Fuel",
            new CalibrationMapAddressRange(startOffset, length),
            isEmissionsRelated,
            [],
            "dummy-test-profile",
            CalibrationMapDataType.UInt8,
            CalibrationMapEndianess.NotApplicable,
            1m,
            "raw",
            0m,
            255m,
            supportStatus);
}
