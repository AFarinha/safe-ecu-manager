namespace SafeEcu.Application.Projects;

public sealed record EcuProjectWorkflowState(
    EcuProjectWorkflowStatus Status,
    string StatusText,
    string NextStep,
    IReadOnlyList<string> CompletedSteps,
    IReadOnlyList<string> MissingSteps,
    IReadOnlyList<string> BlockReasons);
