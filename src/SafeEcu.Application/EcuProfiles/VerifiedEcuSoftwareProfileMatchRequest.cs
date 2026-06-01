namespace SafeEcu.Application.EcuProfiles;

public sealed record VerifiedEcuSoftwareProfileMatchRequest(
    string EcuFamily,
    string HardwareReference,
    string SoftwareReference,
    string SoftwareVersion,
    long FileSizeBytes,
    string OriginalSha256Hash);
