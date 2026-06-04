using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Persistence;

public interface IEcuProjectRepository
{
    Task AddAsync(EcuProject project, CancellationToken cancellationToken = default);

    Task UpdateAsync(EcuProject project, CancellationToken cancellationToken = default);

    Task<EcuProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EcuProject>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EcuProject>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
