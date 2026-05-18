using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuFileValidationService
{
    private readonly IEcuFileRepository _ecuFileRepository;
    private readonly EcuFileValidationOptions _options;

    public EcuFileValidationService(
        IEcuFileRepository ecuFileRepository,
        EcuFileValidationOptions? options = null)
    {
        _ecuFileRepository = ecuFileRepository;
        _options = options ?? new EcuFileValidationOptions();
    }

    public async Task<EcuFileValidationResult> ValidateForImportAsync(
        EcuFileValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new EcuFileValidationResult();

        ValidateVehicleAndEcu(request, result);
        ValidateMetadata(request, result);
        ValidateSourceFile(request, result);
        await ValidateHashAsync(request, result, cancellationToken);

        if (result.IsValid)
        {
            result.AddMessage("ECU file passed basic defensive validation.");
        }

        return result;
    }

    private static void ValidateVehicleAndEcu(EcuFileValidationRequest request, EcuFileValidationResult result)
    {
        if (request.VehicleId == Guid.Empty)
        {
            result.AddError("The ECU file must be associated with a vehicle.");
        }

        if (request.EcuInfoId is null || request.EcuInfoId == Guid.Empty)
        {
            result.AddWarning("No confirmed ECU/EDU is associated with this file. It will remain Unknown until manually identified.");
        }
    }

    private static void ValidateMetadata(EcuFileValidationRequest request, EcuFileValidationResult result)
    {
        if (request.FileType == EcuFileType.Unknown)
        {
            result.AddWarning("ECU file type is Unknown.");
        }

        if (string.IsNullOrWhiteSpace(request.FileOrigin))
        {
            result.AddWarning("ECU file origin is not specified.");
        }

        if (string.IsNullOrWhiteSpace(request.ReadMethod))
        {
            result.AddWarning("ECU file read method is not specified.");
        }

        if (string.IsNullOrWhiteSpace(request.ProgrammerUsed))
        {
            result.AddWarning("Programmer used is not specified.");
        }
    }

    private void ValidateSourceFile(EcuFileValidationRequest request, EcuFileValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(request.SourceFilePath))
        {
            result.AddError("No ECU file was selected.");
            return;
        }

        if (!File.Exists(request.SourceFilePath))
        {
            result.AddError("The selected ECU file does not exist.");
            return;
        }

        var extension = Path.GetExtension(request.SourceFilePath);
        if (!_options.AllowedExtensions.Contains(extension))
        {
            result.AddError("The selected file extension is not supported. Allowed extensions: .bin, .ori, .mod.");
        }

        var fileInfo = new FileInfo(request.SourceFilePath);
        if (fileInfo.Length < _options.MinimumSizeBytes)
        {
            result.AddError("The selected ECU file is empty or too small.");
        }

        if (fileInfo.Length > _options.MaximumSizeBytes)
        {
            result.AddError("The selected ECU file is larger than the configured safety limit.");
        }
    }

    private async Task ValidateHashAsync(
        EcuFileValidationRequest request,
        EcuFileValidationResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sha256Hash))
        {
            result.AddWarning("SHA-256 hash has not been calculated yet.");
            return;
        }

        var existing = await _ecuFileRepository.GetBySha256HashAsync(request.Sha256Hash, cancellationToken);
        if (existing is not null)
        {
            result.AddError("An ECU file with the same SHA-256 hash is already registered.");
        }
    }
}
