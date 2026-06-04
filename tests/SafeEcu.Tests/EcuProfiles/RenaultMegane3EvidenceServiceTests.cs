using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Tests.EcuProfiles;

public sealed class RenaultMegane3EvidenceServiceTests
{
    [Fact]
    public void Evaluate_blocks_when_required_evidence_is_missing()
    {
        var service = new RenaultMegane3EvidenceService();

        var result = service.Evaluate(new RenaultMegane3EcuEvidence(
            K9KVariant: string.Empty,
            EcuManufacturer: "Delphi",
            EcuFamily: "Delphi DCM",
            HardwareReference: string.Empty,
            SoftwareReference: string.Empty,
            SoftwareVersion: string.Empty,
            OriginalFileSizeBytes: null,
            OriginalSha256Hash: string.Empty,
            ChecksumAlgorithmId: string.Empty,
            HasVerifiedMapDefinitions: false,
            HasSafetyLimits: false,
            HasControlledTestFiles: false));

        Assert.False(result.IsComplete);
        Assert.Contains("Exact K9K variant", result.MissingEvidence);
        Assert.Contains("Original SHA-256 hash", result.MissingEvidence);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("blocked", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_blocks_unknown_ecu_family()
    {
        var service = new RenaultMegane3EvidenceService();

        var result = service.Evaluate(CreateCompleteEvidence() with { EcuFamily = "Unknown" });

        Assert.False(result.IsComplete);
        Assert.Contains(result.BlockReasons, reason => reason.Contains("not a supported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_accepts_complete_candidate_evidence_for_next_validation_phase()
    {
        var service = new RenaultMegane3EvidenceService();

        var result = service.Evaluate(CreateCompleteEvidence());

        Assert.True(result.IsComplete);
        Assert.Empty(result.MissingEvidence);
        Assert.Empty(result.BlockReasons);
    }

    private static RenaultMegane3EcuEvidence CreateCompleteEvidence() =>
        new(
            K9KVariant: "K9K exact variant confirmed",
            EcuManufacturer: "Delphi",
            EcuFamily: "Delphi DCM",
            HardwareReference: "HW-123",
            SoftwareReference: "SW-456",
            SoftwareVersion: "1.0",
            OriginalFileSizeBytes: 1024,
            OriginalSha256Hash: new string('a', 64),
            ChecksumAlgorithmId: "validated-test-checksum",
            HasVerifiedMapDefinitions: true,
            HasSafetyLimits: true,
            HasControlledTestFiles: true);
}
