namespace SafeEcu.Application.ControlledParameters;

public sealed record ControlledParameterRule(
    string ProfileKey,
    string ParameterName,
    IReadOnlyList<string> RequiredParameters,
    string Reason);
