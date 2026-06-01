using SafeEcu.Application.Auditing;
using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.Persistence;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Auditing;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class CalibrationPatchPreviewDetailsTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "calibration-patch-preview-details-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Preview_returns_in_memory_before_after_details_without_creating_file()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);
        var service = CreateService();

        var result = await service.PreviewAsync(new CalibrationPatchPreviewRequest(
            filePath,
            new string('a', 64),
            "technical-comparison",
            CalibrationPatchMode.DummyPatch,
            [CreateMap()],
            [new CalibrationChange("FuelQuantity", 1, 0x25, "Dummy preview")],
            [new SafetyParameterProposal("FuelQuantity", 20m, 30m)],
            [CreateLimit("FuelQuantity", 0m, 60m)],
            HasOriginalBackup: true,
            IsChecksumSupported: true,
            AreDependenciesSatisfied: true));

        var item = Assert.Single(result.PreviewItems!);
        Assert.True(result.IsAllowed);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Equal(1, item.AbsoluteOffset);
        Assert.Equal(0x20, item.CurrentValue);
        Assert.Equal(0x25, item.ProposedValue);
        Assert.Equal("FuelQuantity", item.MapId);
        Assert.Equal("Dummy preview", item.Reason);
    }

    [Fact]
    public async Task Preview_includes_declared_map_dependencies_in_preview_items()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);
        var service = CreateService();

        var result = await service.PreviewAsync(new CalibrationPatchPreviewRequest(
            filePath,
            new string('a', 64),
            "technical-comparison",
            CalibrationPatchMode.DummyPatch,
            [CreateMap(requiredMapIds: ["SmokeLimiter"])],
            [new CalibrationChange("FuelQuantity", 1, 0x25, "Dummy preview")],
            [new SafetyParameterProposal("FuelQuantity", 20m, 30m)],
            [CreateLimit("FuelQuantity", 0m, 60m)],
            HasOriginalBackup: true,
            IsChecksumSupported: true,
            AreDependenciesSatisfied: true));

        Assert.False(result.IsAllowed);
        var item = Assert.Single(result.PreviewItems!);
        Assert.Contains("SmokeLimiter", item.RequiredMapIds);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("SmokeLimiter"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private CalibrationPatchPreviewService CreateService()
    {
        var validationService = new CalibrationPatchValidationService(
            new GuidedProfileService(new GuidedCalibrationProfileCatalog()),
            new SafetyLimitEngine());

        return new CalibrationPatchPreviewService(
            new CalibrationMapLocator(),
            validationService,
            new AuditLogService(new InMemoryAuditLogRepository(), new NullAppLogger()));
    }

    private async Task<string> WriteDummyFileAsync(byte[] bytes)
    {
        Directory.CreateDirectory(_testDirectory);
        var path = Path.Combine(_testDirectory, "dummy.bin");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static CalibrationMapDefinition CreateMap(IReadOnlyList<string>? requiredMapIds = null) =>
        new(
            "FuelQuantity",
            "Fuel Quantity",
            "FuelQuantity",
            new CalibrationMapAddressRange(0, 3),
            IsEmissionsRelated: false,
            requiredMapIds ?? []);

    private static SafetyLimit CreateLimit(string parameterName, decimal minValue, decimal maxValue) =>
        new()
        {
            ParameterName = parameterName,
            MinValue = minValue,
            MaxValue = maxValue,
            Unit = "dummy",
            Reason = "Conservative test limit."
        };

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(
            int take = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditLogEntry>>([]);
    }
}
