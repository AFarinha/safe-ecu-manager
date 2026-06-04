using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.MapWorkspace;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class PercentageMapChangePlannerTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "percentage-map-change-planner-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task PlanAsync_creates_byte_changes_for_dummy_verified_maps()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [100, 120, 140]);
        var planner = new PercentageMapChangePlanner();

        var result = await planner.PlanAsync(new PercentageMapChangePlanRequest(
            originalPath,
            "smooth-response",
            10m,
            [CreateMap(length: 3)]));

        Assert.True(result.IsAllowed);
        Assert.Equal(3, result.Changes.Count);
        Assert.Equal([110, 132, 154], result.Changes.Select(change => (int)change.ProposedValue));
        Assert.All(result.Changes, change => Assert.Equal("FuelQuantity", change.MapId));
    }

    [Fact]
    public async Task PlanAsync_blocks_values_above_map_limits()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [250]);
        var planner = new PercentageMapChangePlanner();

        var result = await planner.PlanAsync(new PercentageMapChangePlanRequest(
            originalPath,
            "smooth-response",
            10m,
            [CreateMap(length: 1, max: 255m)]));

        Assert.False(result.IsAllowed);
        Assert.Empty(result.Changes);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("above the maximum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PlanAsync_blocks_real_ecu_profiles()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [100]);
        var planner = new PercentageMapChangePlanner();

        var result = await planner.PlanAsync(new PercentageMapChangePlanRequest(
            originalPath,
            "smooth-response",
            5m,
            [CreateMap(profileId: "renault-real-profile")]));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Real ECU", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PlanAsync_blocks_emissions_related_maps()
    {
        Directory.CreateDirectory(_testDirectory);
        var originalPath = Path.Combine(_testDirectory, "original.bin");
        await File.WriteAllBytesAsync(originalPath, [100]);
        var planner = new PercentageMapChangePlanner();

        var result = await planner.PlanAsync(new PercentageMapChangePlanRequest(
            originalPath,
            "smooth-response",
            5m,
            [CreateMap(isEmissionsRelated: true)]));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("emissions", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private static CalibrationMapDefinition CreateMap(
        int length = 1,
        decimal max = 255m,
        string profileId = "dummy-test-profile",
        bool isEmissionsRelated = false) =>
        new(
            "FuelQuantity",
            "Fuel quantity",
            "Fuel",
            new CalibrationMapAddressRange(0, length),
            isEmissionsRelated,
            [],
            profileId,
            CalibrationMapDataType.UInt8,
            CalibrationMapEndianess.NotApplicable,
            1m,
            "raw",
            0m,
            max,
            CalibrationMapDefinitionSupportStatus.Verified);
}
