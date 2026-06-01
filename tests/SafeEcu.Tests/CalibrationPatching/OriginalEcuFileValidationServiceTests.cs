using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Infrastructure.Files;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class OriginalEcuFileValidationServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "original-ecu-file-validation-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Validate_accepts_dummy_file_matching_profile_hash_size_and_map_ranges()
    {
        var filePath = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04]);
        var hash = await new Sha256FileHashService().ComputeSha256Async(filePath);
        var profile = CreateProfile(expectedFileSize: 4, originalHash: hash);

        var result = await CreateService().ValidateAsync(new OriginalEcuFileValidationRequest(
            filePath,
            filePath,
            profile,
            [CreateMap(length: 4)]));

        Assert.True(result.IsValid);
        Assert.Empty(result.BlockReasons);
    }

    [Fact]
    public async Task Validate_blocks_missing_backup()
    {
        var filePath = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04]);
        var hash = await new Sha256FileHashService().ComputeSha256Async(filePath);

        var result = await CreateService().ValidateAsync(new OriginalEcuFileValidationRequest(
            filePath,
            Path.Combine(_testDirectory, "missing-backup.bin"),
            CreateProfile(expectedFileSize: 4, originalHash: hash),
            [CreateMap(length: 4)]));

        Assert.False(result.IsValid);
        Assert.Contains("backup", result.BlockReasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validate_blocks_size_mismatch()
    {
        var filePath = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04]);
        var hash = await new Sha256FileHashService().ComputeSha256Async(filePath);

        var result = await CreateService().ValidateAsync(new OriginalEcuFileValidationRequest(
            filePath,
            filePath,
            CreateProfile(expectedFileSize: 8, originalHash: hash),
            [CreateMap(length: 4)]));

        Assert.False(result.IsValid);
        Assert.Contains("does not match expected", result.BlockReasons.Single());
    }

    [Fact]
    public async Task Validate_blocks_hash_mismatch()
    {
        var filePath = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04]);

        var result = await CreateService().ValidateAsync(new OriginalEcuFileValidationRequest(
            filePath,
            filePath,
            CreateProfile(expectedFileSize: 4, originalHash: new string('b', 64)),
            [CreateMap(length: 4)]));

        Assert.False(result.IsValid);
        Assert.Contains("SHA-256", result.BlockReasons.Single());
    }

    [Fact]
    public async Task Validate_blocks_map_range_outside_file()
    {
        var filePath = await WriteFileAsync("original.bin", [0x01, 0x02, 0x03, 0x04]);
        var hash = await new Sha256FileHashService().ComputeSha256Async(filePath);

        var result = await CreateService().ValidateAsync(new OriginalEcuFileValidationRequest(
            filePath,
            filePath,
            CreateProfile(expectedFileSize: 4, originalHash: hash),
            [CreateMap(startOffset: 3, length: 2)]));

        Assert.False(result.IsValid);
        Assert.Contains("exceeds original file size", result.BlockReasons.Single());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static OriginalEcuFileValidationService CreateService() =>
        new(new Sha256FileHashService(), new CalibrationMapDefinitionValidationService());

    private async Task<string> WriteFileAsync(string fileName, byte[] bytes)
    {
        Directory.CreateDirectory(_testDirectory);
        var path = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static VerifiedEcuSoftwareProfile CreateProfile(long expectedFileSize, string originalHash) =>
        new(
            "dummy-test-profile",
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            expectedFileSize,
            originalHash,
            ["FuelQuantity"],
            "dummy-checksum",
            VerifiedEcuSoftwareSupportStatus.Verified,
            "Unit test",
            "Dummy-only profile.");

    private static CalibrationMapDefinition CreateMap(long startOffset = 0, int length = 4) =>
        new(
            "FuelQuantity",
            "FuelQuantity",
            "FuelQuantity",
            new CalibrationMapAddressRange(startOffset, length),
            IsEmissionsRelated: false,
            [],
            "dummy-test-profile",
            CalibrationMapDataType.UInt8,
            CalibrationMapEndianess.NotApplicable,
            1m,
            "raw",
            0m,
            255m,
            CalibrationMapDefinitionSupportStatus.Verified);
}
