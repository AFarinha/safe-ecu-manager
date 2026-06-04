using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Persistence;

public interface IEcuFileRepository
{
    Task AddAsync(EcuFile ecuFile, CancellationToken cancellationToken = default);

    Task<EcuFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EcuFile?> GetBySha256HashAsync(string sha256Hash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EcuFile>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
