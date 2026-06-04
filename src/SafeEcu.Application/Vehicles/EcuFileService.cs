using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuFileService
{
    private readonly IEcuFileRepository _ecuFileRepository;
    private readonly IAppLogger _logger;

    public EcuFileService(IEcuFileRepository ecuFileRepository, IAppLogger logger)
    {
        _ecuFileRepository = ecuFileRepository;
        _logger = logger;
    }

    public async Task<OperationResult<EcuFile>> RegisterMetadataAsync(
        EcuFile ecuFile,
        CancellationToken cancellationToken = default)
    {
        if (ecuFile.VehicleId == Guid.Empty)
        {
            return OperationResult<EcuFile>.Failure("The ECU file must be associated with a vehicle.");
        }

        if (string.IsNullOrWhiteSpace(ecuFile.Sha256Hash))
        {
            return OperationResult<EcuFile>.Failure("The ECU file SHA-256 hash is required.");
        }

        var existing = await _ecuFileRepository.GetBySha256HashAsync(ecuFile.Sha256Hash, cancellationToken);
        if (existing is not null)
        {
            return OperationResult<EcuFile>.Failure("An ECU file with the same SHA-256 hash is already registered.");
        }

        ecuFile.ImportedAt = DateTimeOffset.UtcNow;

        await _ecuFileRepository.AddAsync(ecuFile, cancellationToken);
        _logger.Information($"ECU file metadata registered: {ecuFile.Id}.");

        return OperationResult<EcuFile>.Success(ecuFile);
    }

    public Task<IReadOnlyList<EcuFile>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        _ecuFileRepository.ListByVehicleAsync(vehicleId, cancellationToken);

    public Task<EcuFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _ecuFileRepository.GetByIdAsync(id, cancellationToken);

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return OperationResult.Failure("ECU file id is required.");
        }

        try
        {
            await _ecuFileRepository.DeleteAsync(id, cancellationToken);
            _logger.Information($"ECU file metadata deleted: {id}.");
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.Warning($"ECU file delete blocked: {id}. {exception.Message}");
            return OperationResult.Failure("ECU file metadata could not be deleted because it is linked to comparisons, projects or reports.");
        }
    }
}
