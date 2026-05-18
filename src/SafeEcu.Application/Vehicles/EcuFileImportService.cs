using SafeEcu.Application.Common;
using SafeEcu.Application.Files;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuFileImportService
{
    private readonly IEcuInfoRepository _ecuInfoRepository;
    private readonly EcuFileService _ecuFileService;
    private readonly EcuFileValidationService _ecuFileValidationService;
    private readonly IFileHashService _fileHashService;
    private readonly IAppLogger _logger;

    public EcuFileImportService(
        IEcuInfoRepository ecuInfoRepository,
        EcuFileService ecuFileService,
        EcuFileValidationService ecuFileValidationService,
        IFileHashService fileHashService,
        IAppLogger logger)
    {
        _ecuInfoRepository = ecuInfoRepository;
        _ecuFileService = ecuFileService;
        _ecuFileValidationService = ecuFileValidationService;
        _fileHashService = fileHashService;
        _logger = logger;
    }

    public async Task<OperationResult<EcuFile>> ImportAsync(
        EcuFileImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _ecuFileValidationService.ValidateForImportAsync(
            new EcuFileValidationRequest(
                request.VehicleId,
                request.EcuInfoId,
                request.SourceFilePath,
                request.FileType,
                request.FileOrigin,
                request.ReadMethod,
                request.ProgrammerUsed),
            cancellationToken);
        if (!validation.IsValid)
        {
            return OperationResult<EcuFile>.Failure(validation.ToDisplayText());
        }

        var sha256Hash = await _fileHashService.ComputeSha256Async(request.SourceFilePath, cancellationToken);
        var validationWithHash = await _ecuFileValidationService.ValidateForImportAsync(
            new EcuFileValidationRequest(
                request.VehicleId,
                request.EcuInfoId,
                request.SourceFilePath,
                request.FileType,
                request.FileOrigin,
                request.ReadMethod,
                request.ProgrammerUsed,
                sha256Hash),
            cancellationToken);
        if (!validationWithHash.IsValid)
        {
            return OperationResult<EcuFile>.Failure(validationWithHash.ToDisplayText());
        }

        var sourceFileInfo = new FileInfo(request.SourceFilePath);
        var ecuInfoId = request.EcuInfoId ?? await GetOrCreateUnknownEcuInfoAsync(request.VehicleId, cancellationToken);
        var backupPath = CreateBackupPath(request.BackupDirectory, request.VehicleId, sha256Hash, sourceFileInfo.Name);

        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        if (File.Exists(backupPath))
        {
            return OperationResult<EcuFile>.Failure("An ECU file with the same SHA-256 hash is already registered.");
        }

        File.Copy(request.SourceFilePath, backupPath, overwrite: false);

        var ecuFile = new EcuFile
        {
            VehicleId = request.VehicleId,
            EcuInfoId = ecuInfoId,
            FileType = request.FileType,
            FileName = sourceFileInfo.Name,
            FilePath = backupPath,
            SizeBytes = sourceFileInfo.Length,
            Sha256Hash = sha256Hash,
            FileOrigin = request.FileOrigin,
            ReadMethod = request.ReadMethod,
            ProgrammerUsed = request.ProgrammerUsed,
            ChecksumStatus = ChecksumStatus.Unknown,
            Notes = request.Notes
        };

        var result = await _ecuFileService.RegisterMetadataAsync(ecuFile, cancellationToken);
        if (!result.IsSuccess)
        {
            TryDeleteBackupCopy(backupPath);
            return result;
        }

        _logger.Information($"ECU file imported: {ecuFile.Id}, hash {sha256Hash}.");
        return result;
    }

    private async Task<Guid> GetOrCreateUnknownEcuInfoAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var existing = await _ecuInfoRepository.ListByVehicleAsync(vehicleId, cancellationToken);
        var unknownEcu = existing.FirstOrDefault(ecu =>
            string.Equals(ecu.Manufacturer, "Unknown", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ecu.EcuFamily, "Unknown", StringComparison.OrdinalIgnoreCase));

        if (unknownEcu is not null)
        {
            return unknownEcu.Id;
        }

        var ecuInfo = new EcuInfo
        {
            VehicleId = vehicleId,
            Manufacturer = "Unknown",
            EcuFamily = "Unknown",
            SupportStatus = SupportStatus.Unknown,
            Notes = "Automatically created during file import. ECU/EDU identification must be confirmed manually before advanced support."
        };

        await _ecuInfoRepository.AddAsync(ecuInfo, cancellationToken);
        return ecuInfo.Id;
    }

    private static string CreateBackupPath(string backupDirectory, Guid vehicleId, string sha256Hash, string sourceFileName)
    {
        var safeFileName = string.Join("_", sourceFileName.Split(Path.GetInvalidFileNameChars()));
        var prefixedFileName = $"{sha256Hash[..12]}_{safeFileName}";

        return Path.Combine(backupDirectory, vehicleId.ToString("N"), prefixedFileName);
    }

    private void TryDeleteBackupCopy(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
        catch (Exception exception)
        {
            _logger.Warning($"Failed to delete duplicate backup copy '{backupPath}': {exception.Message}");
        }
    }
}
