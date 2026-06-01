using SafeEcu.Application.Auditing;
using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Auditing;
using SafeEcu.Infrastructure.Files;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class DummyCalibrationPatchExportServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "dummy-calibration-patch-export-tests",
        Guid.NewGuid().ToString("N"));

    private readonly InMemoryAuditLogRepository _auditRepository = new();

    [Fact]
    public async Task Export_writes_modified_dummy_file_and_hashes_output()
    {
        var originalPath = await WriteFileAsync("original.bin", [0x10, 0x20, 0x30]);
        var outputDirectory = Path.Combine(_testDirectory, "out");

        var result = await CreateService().ExportAsync(new DummyCalibrationPatchExportRequest(
            originalPath,
            outputDirectory,
            CreateProfile(),
            CreateValidatedChangeSet()));

        Assert.True(result.IsAllowed);
        Assert.True(result.CreatesModifiedFile);
        Assert.True(File.Exists(result.OutputFilePath));
        Assert.Equal([0x10, 0x25, 0x30], await File.ReadAllBytesAsync(result.OutputFilePath!));
        Assert.Equal(await new Sha256FileHashService().ComputeSha256Async(result.OutputFilePath!), result.OutputSha256Hash);
        Assert.Single(await _auditRepository.ListRecentAsync());
    }

    [Fact]
    public async Task Export_blocks_non_dummy_profile()
    {
        var originalPath = await WriteFileAsync("original.bin", [0x10, 0x20, 0x30]);

        var result = await CreateService().ExportAsync(new DummyCalibrationPatchExportRequest(
            originalPath,
            Path.Combine(_testDirectory, "out"),
            CreateProfile(profileId: "real-profile-candidate", source: "Workshop evidence"),
            CreateValidatedChangeSet()));

        Assert.False(result.IsAllowed);
        Assert.False(result.CreatesModifiedFile);
        Assert.Contains("non-dummy", result.BlockReasons.Single());
    }

    [Fact]
    public async Task Export_blocks_without_dummy_checksum_algorithm()
    {
        var originalPath = await WriteFileAsync("original.bin", [0x10, 0x20, 0x30]);

        var result = await CreateService().ExportAsync(new DummyCalibrationPatchExportRequest(
            originalPath,
            Path.Combine(_testDirectory, "out"),
            CreateProfile(checksumAlgorithmId: "real-checksum-placeholder"),
            CreateValidatedChangeSet()));

        Assert.False(result.IsAllowed);
        Assert.Contains("dummy checksum", result.BlockReasons.Single());
    }

    [Fact]
    public async Task Export_blocks_unvalidated_changeset()
    {
        var originalPath = await WriteFileAsync("original.bin", [0x10, 0x20, 0x30]);

        var result = await CreateService().ExportAsync(new DummyCalibrationPatchExportRequest(
            originalPath,
            Path.Combine(_testDirectory, "out"),
            CreateProfile(),
            new ValidatedCalibrationChangeSetResult(false, null, [], ["Blocked"])));

        Assert.False(result.IsAllowed);
        Assert.Contains("Validated change set", result.BlockReasons.Single());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private DummyCalibrationPatchExportService CreateService() =>
        new(new Sha256FileHashService(), new AuditLogService(_auditRepository, new NullAppLogger()));

    private async Task<string> WriteFileAsync(string fileName, byte[] bytes)
    {
        Directory.CreateDirectory(_testDirectory);
        var path = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static VerifiedEcuSoftwareProfile CreateProfile(
        string profileId = "dummy-test-profile",
        string source = "Unit test",
        string checksumAlgorithmId = "dummy-checksum") =>
        new(
            profileId,
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            3,
            new string('a', 64),
            ["FuelQuantity"],
            checksumAlgorithmId,
            VerifiedEcuSoftwareSupportStatus.Verified,
            source,
            "Dummy-only profile.");

    private static ValidatedCalibrationChangeSetResult CreateValidatedChangeSet() =>
        new(
            true,
            new CalibrationChangeSet(
                "technical-comparison",
                CalibrationPatchMode.DummyPatch,
                [new CalibrationMapValue("FuelQuantity", 1, 0x20, 0x25)],
                CreatesModifiedFile: false),
            ["Validated"],
            []);

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
            Task.FromResult<IReadOnlyList<AuditLogEntry>>(_entries.Take(take).ToList());
    }
}
