using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Reports;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Auditing;
using SafeEcu.Infrastructure.Reports;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Reports;

public sealed class HtmlCalibrationPatchReportServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "calibration-patch-report-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Generate_writes_patch_report_with_required_sections()
    {
        var service = new HtmlCalibrationPatchReportService(new NullAppLogger());

        var result = await service.GenerateAsync(CreateRequest());

        Assert.True(result.IsSuccess);
        var html = await File.ReadAllTextAsync(result.Value!);
        Assert.Contains("Safe ECU Calibration Patch Report", html);
        Assert.Contains("Vehicle", html);
        Assert.Contains("ECU/Software Profile", html);
        Assert.Contains("SHA-256", html);
        Assert.Contains("Fuel Quantity", html);
        Assert.Contains("Safety", html);
        Assert.Contains("Checksum", html);
        Assert.Contains("Audit", html);
        Assert.Contains("does not approve ECU writing or flashing", html);
    }

    [Fact]
    public async Task Generate_marks_blocked_gate_when_checksum_is_not_supported()
    {
        var request = CreateRequest(checksumSupported: false);

        var result = await new HtmlCalibrationPatchReportService(new NullAppLogger()).GenerateAsync(request);

        var html = await File.ReadAllTextAsync(result.Value!);
        Assert.Contains("No real calibration output is allowed", html);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private CalibrationPatchReportRequest CreateRequest(bool checksumSupported = true) =>
        new(
            _testDirectory,
            "Opel Corsa C 1.7 DTI",
            "Dummy ECU profile",
            "original.bin",
            new string('a', 64),
            CreateProfile(),
            CreatePatchPreview(),
            CreateSafetyValidation(),
            CreateChecksumSupport(checksumSupported),
            [new AuditLogEntry { Action = "CalibrationPatch.Preview", Severity = "Information", Message = "Preview generated." }]);

    private static VerifiedEcuSoftwareProfile CreateProfile() =>
        new(
            "dummy-test-profile",
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            3,
            new string('a', 64),
            ["FuelQuantity"],
            "dummy-checksum",
            VerifiedEcuSoftwareSupportStatus.Verified,
            "Unit test",
            "Dummy-only profile.");

    private static CalibrationPatchResult CreatePatchPreview() =>
        new(
            true,
            CalibrationPatchStatus.AllowedPreview,
            new CalibrationChangeSet("technical-comparison", CalibrationPatchMode.DummyPatch, [], false),
            ["Preview OK"],
            [],
            [new CalibrationPatchPreviewItem("FuelQuantity", "Fuel Quantity", "FuelQuantity", 1, 0x20, 0x25, [], "Dummy")]);

    private static CalibrationValidationResult CreateSafetyValidation() =>
        new(true, CalibrationSafetyStatus.Safe, ["Safe"], [], [], [], []);

    private static EcuSoftwareChecksumSupportResult CreateChecksumSupport(bool supported) =>
        new(
            supported,
            supported,
            supported ? ChecksumValidationStatus.Valid : ChecksumValidationStatus.NotSupported,
            supported ? "dummy-checksum" : "None",
            supported ? ["Checksum OK"] : [],
            supported ? [] : ["Checksum missing"]);
}
