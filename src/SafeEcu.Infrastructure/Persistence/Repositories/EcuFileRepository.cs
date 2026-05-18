using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class EcuFileRepository : IEcuFileRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public EcuFileRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(EcuFile ecuFile, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.EcuFiles.Add(ecuFile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EcuFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(file => file.Id == id, cancellationToken);
    }

    public async Task<EcuFile?> GetBySha256HashAsync(
        string sha256Hash,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(file => file.Sha256Hash == sha256Hash, cancellationToken);
    }

    public async Task<IReadOnlyList<EcuFile>> ListByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        var files = await dbContext.EcuFiles
            .Where(file => file.VehicleId == vehicleId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return files
            .OrderByDescending(file => file.ImportedAt)
            .ToList();
    }
}
