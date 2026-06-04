namespace SafeEcu.Application.EcuProfiles;

public sealed record RenaultMegane3EcuEvidence(
    string K9KVariant,
    string EcuManufacturer,
    string EcuFamily,
    string HardwareReference,
    string SoftwareReference,
    string SoftwareVersion,
    long? OriginalFileSizeBytes,
    string OriginalSha256Hash,
    string ChecksumAlgorithmId,
    bool HasVerifiedMapDefinitions,
    bool HasSafetyLimits,
    bool HasControlledTestFiles);
