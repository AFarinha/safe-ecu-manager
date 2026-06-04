using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.MapWorkspace;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class MapChangeExportServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "map-change-export-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExportAsync_writes_modified_file_when_all_gates_pass()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        var outputDirectory = Path.Combine(_testDirectory, "out");
        await File.WriteAllBytesAsync(originalPath, [0x10, 0x20, 0x30]);
        var service = new MapChangeExportService();

        var result = await service.ExportAsync(new MapChangeExportRequest(
            originalPath,
            outputDirectory,
            "modified.bin",
            [CreateMap(length: 3)],
            [new CalibrationChange("FuelQuantity", 1, 0x25, "Test change")],
            new MapChangeExportGateResult(true, ["Allowed."], []),
            ChecksumRecalculated: true));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.OutputFilePath);
        Assert.True(File.Exists(result.OutputFilePath));
        Assert.Equal([0x10, 0x25, 0x30], await File.ReadAllBytesAsync(result.OutputFilePath!));
        Assert.False(string.IsNullOrWhiteSpace(result.Sha256Hash));
    }

    [Fact]
    public async Task ExportAsync_blocks_when_checksum_was_not_recalculated()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [0x10]);
        var service = new MapChangeExportService();

        var result = await service.ExportAsync(new MapChangeExportRequest(
            originalPath,
            _testDirectory,
            "modified.bin",
            [CreateMap()],
            [new CalibrationChange("FuelQuantity", 0, 0x11, "Test change")],
            new MapChangeExportGateResult(true, ["Allowed."], []),
            ChecksumRecalculated: false));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Checksum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExportAsync_blocks_real_ecu_maps()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [0x10]);
        var service = new MapChangeExportService();

        var result = await service.ExportAsync(new MapChangeExportRequest(
            originalPath,
            _testDirectory,
            "modified.bin",
            [CreateMap(profileId: "renault-real-profile")],
            [new CalibrationChange("FuelQuantity", 0, 0x11, "Test change")],
            new MapChangeExportGateResult(true, ["Allowed."], []),
            ChecksumRecalculated: true));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Real ECU", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static CalibrationMapDefinition CreateMap(int length = 1, string profileId = "dummy-test-profile") =>
        new(
            "FuelQuantity",
            "Fuel quantity",
            "Fuel",
            new CalibrationMapAddressRange(0, length),
            IsEmissionsRelated: false,
            [],
            profileId,
            CalibrationMapDataType.UInt8,
            CalibrationMapEndianess.NotApplicable,
            1m,
            "raw",
            0m,
            255m,
            CalibrationMapDefinitionSupportStatus.Verified);
}
