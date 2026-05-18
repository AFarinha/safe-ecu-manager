using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public VehicleRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.Vehicles.Update(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.Vehicles
            .Include(vehicle => vehicle.Ecus)
            .Include(vehicle => vehicle.EcuFiles)
            .SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.Vehicles
            .OrderBy(vehicle => vehicle.Make)
            .ThenBy(vehicle => vehicle.Model)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
