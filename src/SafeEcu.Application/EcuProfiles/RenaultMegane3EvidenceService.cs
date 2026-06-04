using System.Text.RegularExpressions;

namespace SafeEcu.Application.EcuProfiles;

public sealed class RenaultMegane3EvidenceService
{
    private static readonly Regex Sha256Regex = new("^[a-fA-F0-9]{64}$", RegexOptions.Compiled);

    public RenaultMegane3EvidenceResult Evaluate(RenaultMegane3EcuEvidence evidence)
    {
        var missing = new List<string>();
        var blockReasons = new List<string>();

        RequireText(evidence.K9KVariant, "Exact K9K variant", missing);
        RequireText(evidence.EcuManufacturer, "ECU manufacturer", missing);
        RequireText(evidence.EcuFamily, "ECU family", missing);
        RequireText(evidence.HardwareReference, "ECU hardware reference", missing);
        RequireText(evidence.SoftwareReference, "ECU software reference", missing);
        RequireText(evidence.SoftwareVersion, "ECU software version", missing);
        RequireText(evidence.ChecksumAlgorithmId, "Validated checksum algorithm id", missing);

        if (evidence.OriginalFileSizeBytes is null or <= 0)
        {
            missing.Add("Original file size");
        }

        if (string.IsNullOrWhiteSpace(evidence.OriginalSha256Hash))
        {
            missing.Add("Original SHA-256 hash");
        }
        else if (!Sha256Regex.IsMatch(evidence.OriginalSha256Hash.Trim()))
        {
            blockReasons.Add("Original SHA-256 hash must be 64 hexadecimal characters.");
        }

        if (!evidence.HasVerifiedMapDefinitions)
        {
            missing.Add("Verified map definitions");
        }

        if (!evidence.HasSafetyLimits)
        {
            missing.Add("Renault/K9K safety limits");
        }

        if (!evidence.HasControlledTestFiles)
        {
            missing.Add("Controlled test files");
        }

        if (missing.Count > 0)
        {
            blockReasons.Add("Renault Megane 3 calibration support remains blocked until all required evidence is complete.");
        }

        if (!IsSupportedCandidateFamily(evidence.EcuFamily))
        {
            blockReasons.Add("ECU family is not a supported Renault Megane 3 candidate family.");
        }

        IReadOnlyList<string> messages = missing.Count == 0 && blockReasons.Count == 0
            ? ["Renault Megane 3 evidence is complete enough for the next validation phase."]
            : ["Renault Megane 3 evidence is incomplete. File management remains available; calibration output is blocked."];

        return new RenaultMegane3EvidenceResult(
            missing.Count == 0 && blockReasons.Count == 0,
            messages,
            missing,
            blockReasons);
    }

    private static void RequireText(string value, string label, ICollection<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(label);
        }
    }

    private static bool IsSupportedCandidateFamily(string ecuFamily) =>
        ecuFamily.Contains("Delphi DCM", StringComparison.OrdinalIgnoreCase)
        || ecuFamily.Contains("Bosch EDC", StringComparison.OrdinalIgnoreCase);
}
