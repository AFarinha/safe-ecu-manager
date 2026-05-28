using SafeEcu.Domain.Application;

namespace SafeEcu.Application.Vehicles;

public sealed record EcuIdentificationResult(
    EcuIdentificationConfidence Confidence,
    SupportStatus SupportStatus,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> MissingEvidence);
