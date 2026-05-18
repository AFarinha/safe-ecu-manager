using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class VehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IAppLogger _logger;

    public VehicleService(IVehicleRepository vehicleRepository, IAppLogger logger)
    {
        _vehicleRepository = vehicleRepository;
        _logger = logger;
    }

    public async Task<OperationResult<Vehicle>> CreateAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Make))
        {
            return OperationResult<Vehicle>.Failure("Vehicle make is required.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.Model))
        {
            return OperationResult<Vehicle>.Failure("Vehicle model is required.");
        }

        vehicle.CreatedAt = DateTimeOffset.UtcNow;
        vehicle.UpdatedAt = vehicle.CreatedAt;

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        _logger.Information($"Vehicle created: {vehicle.Id}.");

        return OperationResult<Vehicle>.Success(vehicle);
    }

    public Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default) =>
        _vehicleRepository.ListAsync(cancellationToken);
}
