using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed record EcuFileValidationRequest(
    Guid VehicleId,
    Guid? EcuInfoId,
    string? SourceFilePath,
    EcuFileType FileType,
    string? FileOrigin,
    string? ReadMethod,
    string? ProgrammerUsed,
    string? Sha256Hash = null);
