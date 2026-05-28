namespace SafeEcu.Application.GuidedProfiles;

public sealed record GuidedProfileAvailability(
    string ProfileKey,
    bool IsAvailable,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
