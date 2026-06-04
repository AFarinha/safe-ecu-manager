using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuInfoService
{
    private readonly IEcuInfoRepository _ecuInfoRepository;
    private readonly IAppLogger _logger;
    private readonly EcuIdentificationService _identificationService;

    public EcuInfoService(
        IEcuInfoRepository ecuInfoRepository,
        IAppLogger logger,
        EcuIdentificationService? identificationService = null)
    {
        _ecuInfoRepository = ecuInfoRepository;
        _logger = logger;
        _identificationService = identificationService ?? new EcuIdentificationService();
    }

    public async Task<OperationResult<EcuInfo>> CreateAsync(
        EcuInfo ecuInfo,
        CancellationToken cancellationToken = default)
    {
        if (ecuInfo.VehicleId == Guid.Empty)
        {
            return OperationResult<EcuInfo>.Failure("The ECU/EDU must be associated with a vehicle.");
        }

        ecuInfo.CreatedAt = DateTimeOffset.UtcNow;
        ecuInfo.UpdatedAt = ecuInfo.CreatedAt;

        await _ecuInfoRepository.AddAsync(ecuInfo, cancellationToken);
        _logger.Information($"ECU info created: {ecuInfo.Id}.");

        return OperationResult<EcuInfo>.Success(ecuInfo);
    }

    public async Task<OperationResult<EcuInfo>> UpdateAsync(
        EcuInfo ecuInfo,
        CancellationToken cancellationToken = default)
    {
        if (ecuInfo.Id == Guid.Empty)
        {
            return OperationResult<EcuInfo>.Failure("ECU/EDU id is required.");
        }

        if (ecuInfo.VehicleId == Guid.Empty)
        {
            return OperationResult<EcuInfo>.Failure("The ECU/EDU must be associated with a vehicle.");
        }

        var existing = await _ecuInfoRepository.GetByIdAsync(ecuInfo.Id, cancellationToken);
        if (existing is null)
        {
            return OperationResult<EcuInfo>.Failure("ECU/EDU was not found.");
        }

        existing.Manufacturer = Normalize(ecuInfo.Manufacturer);
        existing.EcuFamily = Normalize(ecuInfo.EcuFamily);
        existing.HardwareReference = Normalize(ecuInfo.HardwareReference);
        existing.SoftwareReference = Normalize(ecuInfo.SoftwareReference);
        existing.SoftwareVersion = Normalize(ecuInfo.SoftwareVersion);
        existing.Protocol = Normalize(ecuInfo.Protocol);
        existing.SupportStatus = ecuInfo.SupportStatus;
        existing.Notes = Normalize(ecuInfo.Notes);
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _ecuInfoRepository.UpdateAsync(existing, cancellationToken);
        _logger.Information($"ECU info updated: {existing.Id}.");

        return OperationResult<EcuInfo>.Success(existing);
    }

    public Task<EcuInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _ecuInfoRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<EcuInfo>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        _ecuInfoRepository.ListByVehicleAsync(vehicleId, cancellationToken);

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return OperationResult.Failure("ECU/EDU id is required.");
        }

        try
        {
            await _ecuInfoRepository.DeleteAsync(id, cancellationToken);
            _logger.Information($"ECU info deleted: {id}.");
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.Warning($"ECU info delete blocked: {id}. {exception.Message}");
            return OperationResult.Failure("ECU/EDU could not be deleted because it is linked to files, projects or audit history.");
        }
    }

    public EcuIdentificationResult EvaluateIdentification(EcuInfo ecuInfo) =>
        _identificationService.Evaluate(ecuInfo);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
