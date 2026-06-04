namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapEditPreviewCell(
    int Row,
    int Column,
    long Offset,
    decimal CurrentValue,
    decimal ProposedValue,
    decimal Delta);
