namespace SafeEcu.Application.Safety;

public sealed record SafetyLimitProfileValidationRequest(
    string ProfileId,
    IReadOnlyList<SafetyParameterProposal> Proposals);
