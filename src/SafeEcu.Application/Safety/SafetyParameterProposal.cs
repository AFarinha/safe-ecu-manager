namespace SafeEcu.Application.Safety;

public sealed record SafetyParameterProposal(
    string ParameterName,
    decimal CurrentValue,
    decimal ProposedValue);
