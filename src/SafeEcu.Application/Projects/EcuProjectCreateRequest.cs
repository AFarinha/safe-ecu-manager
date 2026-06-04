namespace SafeEcu.Application.Projects;

public sealed record EcuProjectCreateRequest(
    Guid VehicleId,
    Guid OriginalFileId,
    string Name,
    string Make,
    string Model,
    string Engine,
    int? Year,
    string EcuType,
    string? EcuReference,
    string ReadType,
    string? Notes,
    string? Tags);
