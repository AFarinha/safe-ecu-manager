using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Persistence;

public interface IEcuInfoRepository
{
    Task AddAsync(EcuInfo ecuInfo, CancellationToken cancellationToken = default);

    Task<EcuInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EcuInfo>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
