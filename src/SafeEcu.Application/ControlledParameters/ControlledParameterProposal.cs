namespace SafeEcu.Application.ControlledParameters;

public sealed record ControlledParameterProposal(
    string ProfileKey,
    IReadOnlyList<string> ParameterNames);
