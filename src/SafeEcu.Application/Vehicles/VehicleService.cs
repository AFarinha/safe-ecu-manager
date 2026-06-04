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
        var validation = Validate(vehicle);
        if (!validation.IsSuccess)
        {
            return OperationResult<Vehicle>.Failure(validation.ErrorMessage!);
        }

        vehicle.CreatedAt = DateTimeOffset.UtcNow;
        vehicle.UpdatedAt = vehicle.CreatedAt;

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        _logger.Information($"Vehicle created: {vehicle.Id}.");

        return OperationResult<Vehicle>.Success(vehicle);
    }

    public async Task<OperationResult<Vehicle>> UpdateAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        if (vehicle.Id == Guid.Empty)
        {
            return OperationResult<Vehicle>.Failure("Vehicle id is required.");
        }

        var existing = await _vehicleRepository.GetByIdAsync(vehicle.Id, cancellationToken);
        if (existing is null)
        {
            return OperationResult<Vehicle>.Failure("Vehicle was not found.");
        }

        var validation = Validate(vehicle);
        if (!validation.IsSuccess)
        {
            return OperationResult<Vehicle>.Failure(validation.ErrorMessage!);
        }

        existing.Vin = vehicle.Vin;
        existing.LicensePlate = vehicle.LicensePlate;
        existing.Make = vehicle.Make.Trim();
        existing.Model = vehicle.Model.Trim();
        existing.Year = vehicle.Year;
        existing.Engine = vehicle.Engine.Trim();
        existing.EngineCode = vehicle.EngineCode.Trim();
        existing.FuelType = vehicle.FuelType;
        existing.Notes = vehicle.Notes;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _vehicleRepository.UpdateAsync(existing, cancellationToken);
        _logger.Information($"Vehicle updated: {existing.Id}.");

        return OperationResult<Vehicle>.Success(existing);
    }

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _vehicleRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default) =>
        _vehicleRepository.ListAsync(cancellationToken);

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return OperationResult.Failure("Vehicle id is required.");
        }

        try
        {
            await _vehicleRepository.DeleteAsync(id, cancellationToken);
            _logger.Information($"Vehicle deleted: {id}.");
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.Warning($"Vehicle delete blocked: {id}. {exception.Message}");
            return OperationResult.Failure("Vehicle could not be deleted because it is linked to ECU records, files, projects or audit history.");
        }
    }

    private static OperationResult Validate(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Make))
        {
            return OperationResult.Failure("Vehicle make is required.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.Model))
        {
            return OperationResult.Failure("Vehicle model is required.");
        }

        if (vehicle.Year is < 1886 or > 2100)
        {
            return OperationResult.Failure("Vehicle year is outside the supported range.");
        }

        return OperationResult.Success();
    }
}
