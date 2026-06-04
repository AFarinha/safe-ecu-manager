using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class EcuInfoRepository : IEcuInfoRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public EcuInfoRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(EcuInfo ecuInfo, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.EcuInfos.Add(ecuInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EcuInfo ecuInfo, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.EcuInfos.Update(ecuInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EcuInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuInfos
            .AsNoTracking()
            .SingleOrDefaultAsync(ecu => ecu.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EcuInfo>> ListByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuInfos
            .Where(ecu => ecu.VehicleId == vehicleId)
            .OrderBy(ecu => ecu.Manufacturer)
            .ThenBy(ecu => ecu.EcuFamily)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        var ecuInfo = await dbContext.EcuInfos.SingleOrDefaultAsync(ecu => ecu.Id == id, cancellationToken);
        if (ecuInfo is null)
        {
            return;
        }

        dbContext.EcuInfos.Remove(ecuInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
