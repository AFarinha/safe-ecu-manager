namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapSnapshotCell(
    int Row,
    int Column,
    long Offset,
    long RawValue,
    decimal ConvertedValue);
