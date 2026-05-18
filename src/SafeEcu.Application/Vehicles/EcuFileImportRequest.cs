using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed record EcuFileImportRequest(
    Guid VehicleId,
    Guid? EcuInfoId,
    string SourceFilePath,
    string BackupDirectory,
    EcuFileType FileType,
    string? FileOrigin,
    string? ReadMethod,
    string? ProgrammerUsed,
    string? Notes);
