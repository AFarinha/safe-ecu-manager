namespace SafeEcu.Application.EcuProfiles;

public sealed record RenaultMegane3EvidenceResult(
    bool IsComplete,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> MissingEvidence,
    IReadOnlyList<string> BlockReasons);
