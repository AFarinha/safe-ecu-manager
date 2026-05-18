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
}
