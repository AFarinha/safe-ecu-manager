namespace SafeEcu.Application.EcuProfiles;

public sealed record VerifiedEcuSoftwareProfile(
    string ProfileId,
    string EcuFamily,
    string HardwareReference,
    string SoftwareReference,
    string SoftwareVersion,
    long ExpectedFileSizeBytes,
    string OriginalSha256Hash,
    IReadOnlyList<string> AllowedMapIds,
    string ChecksumAlgorithmId,
    VerifiedEcuSoftwareSupportStatus SupportStatus,
    string Source,
    string Notes);
