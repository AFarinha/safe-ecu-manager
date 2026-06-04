using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.MapWorkspace;
using SafeEcu.Application.Safety;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class MapChangeExportGateServiceTests
{
    [Fact]
    public void Evaluate_blocks_when_checksum_cannot_export()
    {
        var service = new MapChangeExportGateService();

        var result = service.Evaluate(new MapChangeExportGateRequest(
            CompleteEvidence(),
            AllowedPlan(),
            new EcuSoftwareChecksumSupportResult(false, false, ChecksumValidationStatus.NotSupported, string.Empty, [], ["Checksum not supported."]),
            AllowedSafety()));

        Assert.False(result.CanExport);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Checksum", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_blocks_when_evidence_is_missing()
    {
        var service = new MapChangeExportGateService();

        var result = service.Evaluate(new MapChangeExportGateRequest(
            new RenaultMegane3EvidenceResult(false, [], ["Software version"], ["Evidence incomplete."]),
            AllowedPlan(),
            SupportedChecksum(),
            AllowedSafety()));

        Assert.False(result.CanExport);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("Software version", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_allows_export_when_all_gates_pass()
    {
        var service = new MapChangeExportGateService();

        var result = service.Evaluate(new MapChangeExportGateRequest(
            CompleteEvidence(),
            AllowedPlan(),
            SupportedChecksum(),
            AllowedSafety()));

        Assert.True(result.CanExport);
        Assert.Empty(result.BlockReasons);
    }

    private static RenaultMegane3EvidenceResult CompleteEvidence() =>
        new(true, ["Evidence complete."], [], []);

    private static PercentageMapChangePlanResult AllowedPlan() =>
        new(true, [], ["Plan allowed."], []);

    private static EcuSoftwareChecksumSupportResult SupportedChecksum() =>
        new(true, true, ChecksumValidationStatus.Valid, "dummy-checksum", ["Checksum supported."], []);

    private static CalibrationValidationResult AllowedSafety() =>
        new(true, CalibrationSafetyStatus.Safe, ["Safety validation passed."], [], [], [], []);
}
