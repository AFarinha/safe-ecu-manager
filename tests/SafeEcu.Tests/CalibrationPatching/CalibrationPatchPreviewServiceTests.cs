using SafeEcu.Application.Auditing;
using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.Persistence;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Auditing;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class CalibrationPatchPreviewServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "calibration-patch-preview-tests",
        Guid.NewGuid().ToString("N"));

    private readonly InMemoryAuditLogRepository _auditRepository = new();

    [Fact]
    public async Task Locator_finds_known_map_value_in_dummy_file()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30, 0x40]);
        var locator = new CalibrationMapLocator();
        var definition = CreateMap("FuelQuantity", startOffset: 1, length: 2);

        var value = await locator.LocateAsync(
            filePath,
            definition,
            new CalibrationChange("FuelQuantity", RelativeOffset: 1, ProposedValue: 0x35, "Dummy preview"));

        Assert.NotNull(value);
        Assert.Equal(2, value.Offset);
        Assert.Equal(0x30, value.CurrentValue);
        Assert.Equal(0x35, value.ProposedValue);
    }

    [Fact]
    public async Task Preview_blocks_unknown_map()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);
        var service = CreateService();

        var result = await service.PreviewAsync(CreateRequest(
            filePath,
            mapDefinitions: [CreateMap("FuelQuantity", 0, 3)],
            changes: [new CalibrationChange("UnknownMap", 0, 0x22, "Unknown")]));

        Assert.False(result.IsAllowed);
        Assert.Contains("not defined", result.BlockReasons.Single());
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Single(await _auditRepository.ListRecentAsync());
    }

    [Fact]
    public async Task Preview_blocks_when_checksum_is_not_supported()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);

        var result = await CreateService().PreviewAsync(CreateRequest(
            filePath,
            isChecksumSupported: false));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Checksum support", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_blocks_when_safety_limit_rejects_value()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);

        var result = await CreateService().PreviewAsync(CreateRequest(
            filePath,
            safetyProposals: [new SafetyParameterProposal("FuelQuantity", 20m, 80m)],
            safetyLimits: [CreateLimit("FuelQuantity", 0m, 60m)]));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Conservative test limit"));
    }

    [Fact]
    public async Task Preview_blocks_profile_that_is_not_supported()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);

        var result = await CreateService().PreviewAsync(CreateRequest(
            filePath,
            profileKey: "conservative-torque"));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Preview_blocks_emissions_related_map()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);
        var emissionsMap = CreateMap("EgrSwitch", 0, 3, isEmissionsRelated: true);

        var result = await CreateService().PreviewAsync(CreateRequest(
            filePath,
            mapDefinitions: [emissionsMap],
            changes: [new CalibrationChange("EgrSwitch", 0, 0x00, "Blocked emissions change")]));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("emissions-related"));
    }

    [Fact]
    public async Task Preview_allows_dummy_patch_preview_without_creating_modified_file()
    {
        var filePath = await WriteDummyFileAsync([0x10, 0x20, 0x30]);

        var result = await CreateService().PreviewAsync(CreateRequest(filePath));

        Assert.True(result.IsAllowed);
        Assert.Equal(CalibrationPatchStatus.AllowedPreview, result.Status);
        Assert.False(result.ChangeSet.CreatesModifiedFile);
        Assert.Single(result.ChangeSet.Values);
        Assert.Single(await _auditRepository.ListRecentAsync());
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
        var guidedProfileService = new GuidedProfileService(new GuidedCalibrationProfileCatalog());
        var safetyLimitEngine = new SafetyLimitEngine();
        var validationService = new CalibrationPatchValidationService(guidedProfileService, safetyLimitEngine);
        var auditLogService = new AuditLogService(_auditRepository, new NullAppLogger());

        return new CalibrationPatchPreviewService(
            new CalibrationMapLocator(),
            validationService,
            auditLogService);
    }

    private CalibrationPatchPreviewRequest CreateRequest(
        string filePath,
        string profileKey = "technical-comparison",
        IReadOnlyList<CalibrationMapDefinition>? mapDefinitions = null,
        IReadOnlyList<CalibrationChange>? changes = null,
        IReadOnlyList<SafetyParameterProposal>? safetyProposals = null,
        IReadOnlyList<SafetyLimit>? safetyLimits = null,
        bool isChecksumSupported = true) =>
        new(
            filePath,
            new string('a', 64),
            profileKey,
            CalibrationPatchMode.DummyPatch,
            mapDefinitions ?? [CreateMap("FuelQuantity", 0, 3)],
            changes ?? [new CalibrationChange("FuelQuantity", 1, 0x25, "Dummy patch preview")],
            safetyProposals ?? [new SafetyParameterProposal("FuelQuantity", 20m, 30m)],
            safetyLimits ?? [CreateLimit("FuelQuantity", 0m, 60m)],
            HasOriginalBackup: true,
            isChecksumSupported,
            AreDependenciesSatisfied: true);

    private async Task<string> WriteDummyFileAsync(byte[] bytes)
    {
        Directory.CreateDirectory(_testDirectory);
        var path = Path.Combine(_testDirectory, "dummy.bin");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static CalibrationMapDefinition CreateMap(
        string mapId,
        long startOffset,
        int length,
        bool isEmissionsRelated = false) =>
        new(
            mapId,
            mapId,
            mapId,
            new CalibrationMapAddressRange(startOffset, length),
            isEmissionsRelated,
            []);

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
        private readonly List<AuditLogEntry> _entries = [];

        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(
            int take = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditLogEntry>>(
                _entries
                    .OrderByDescending(entry => entry.Timestamp)
                    .Take(take)
                    .ToList());
    }
}
