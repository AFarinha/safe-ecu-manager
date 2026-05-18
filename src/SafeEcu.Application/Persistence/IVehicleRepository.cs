using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Persistence;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default);
}
