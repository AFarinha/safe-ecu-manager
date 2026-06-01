namespace SafeEcu.Application.EcuProfiles;

public sealed class VerifiedEcuSoftwareProfileService
{
    private readonly VerifiedEcuSoftwareProfileCatalog _catalog;

    public VerifiedEcuSoftwareProfileService(VerifiedEcuSoftwareProfileCatalog catalog)
    {
        _catalog = catalog;
    }

    public VerifiedEcuSoftwareProfileMatchResult Match(VerifiedEcuSoftwareProfileMatchRequest request)
    {
        var missing = GetMissingFields(request).ToList();
        if (missing.Count > 0)
        {
            return Block(null, $"Missing required ECU/software evidence: {string.Join(", ", missing)}.");
        }

        var profile = _catalog.Profiles.SingleOrDefault(candidate =>
            EqualsText(candidate.EcuFamily, request.EcuFamily)
            && EqualsText(candidate.HardwareReference, request.HardwareReference)
            && EqualsText(candidate.SoftwareReference, request.SoftwareReference)
            && EqualsText(candidate.SoftwareVersion, request.SoftwareVersion)
            && candidate.ExpectedFileSizeBytes == request.FileSizeBytes
            && EqualsText(candidate.OriginalSha256Hash, request.OriginalSha256Hash));

        if (profile is null)
        {
            return Block(null, "No verified ECU/software profile matches the supplied evidence.");
        }

        if (profile.SupportStatus != VerifiedEcuSoftwareSupportStatus.Verified)
        {
            return Block(profile, $"Matched profile is {profile.SupportStatus}; verified support is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.ChecksumAlgorithmId))
        {
            return Block(profile, "Verified profile must define a checksum algorithm id.");
        }

        if (profile.AllowedMapIds.Count == 0)
        {
            return Block(profile, "Verified profile must define at least one allowed map id.");
        }

        return new VerifiedEcuSoftwareProfileMatchResult(
            true,
            true,
            profile,
            [$"Verified ECU/software profile '{profile.ProfileId}' matched."],
            []);
    }

    private static VerifiedEcuSoftwareProfileMatchResult Block(
        VerifiedEcuSoftwareProfile? profile,
        string reason) =>
        new(false, false, profile, [], [reason]);

    private static IEnumerable<string> GetMissingFields(VerifiedEcuSoftwareProfileMatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EcuFamily))
        {
            yield return "ECU family";
        }

        if (string.IsNullOrWhiteSpace(request.HardwareReference))
        {
            yield return "hardware reference";
        }

        if (string.IsNullOrWhiteSpace(request.SoftwareReference))
        {
            yield return "software reference";
        }

        if (string.IsNullOrWhiteSpace(request.SoftwareVersion))
        {
            yield return "software version";
        }

        if (request.FileSizeBytes <= 0)
        {
            yield return "file size";
        }

        if (string.IsNullOrWhiteSpace(request.OriginalSha256Hash))
        {
            yield return "original SHA-256 hash";
        }
    }

    private static bool EqualsText(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
