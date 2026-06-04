using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Projects;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class EcuProjectMapDefinitionRepository : IEcuProjectMapDefinitionRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public EcuProjectMapDefinitionRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(EcuProjectMapDefinition definition, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.EcuProjectMapDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EcuProjectMapDefinition?> GetByProjectAndMapIdAsync(
        Guid projectId,
        string mapId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuProjectMapDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                definition => definition.ProjectId == projectId && definition.MapId == mapId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<EcuProjectMapDefinition>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        var definitions = await dbContext.EcuProjectMapDefinitions
            .Where(definition => definition.ProjectId == projectId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return definitions
            .OrderBy(definition => definition.StartOffset)
            .ThenBy(definition => definition.MapId)
            .ToList();
    }
}
