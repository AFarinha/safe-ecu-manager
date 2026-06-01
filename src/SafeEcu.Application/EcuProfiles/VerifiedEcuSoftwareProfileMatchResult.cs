namespace SafeEcu.Application.EcuProfiles;

public sealed record VerifiedEcuSoftwareProfileMatchResult(
    bool IsMatch,
    bool IsVerified,
    VerifiedEcuSoftwareProfile? Profile,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
