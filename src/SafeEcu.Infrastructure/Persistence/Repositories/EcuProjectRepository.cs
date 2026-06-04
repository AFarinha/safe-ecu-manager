using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Projects;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class EcuProjectRepository : IEcuProjectRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public EcuProjectRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(EcuProject project, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.EcuProjects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EcuProject project, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        var existing = await dbContext.EcuProjects
            .Include(savedProject => savedProject.Versions)
            .SingleAsync(savedProject => savedProject.Id == project.Id, cancellationToken);

        existing.Name = project.Name;
        existing.Make = project.Make;
        existing.Model = project.Model;
        existing.Engine = project.Engine;
        existing.Year = project.Year;
        existing.EcuType = project.EcuType;
        existing.EcuReference = project.EcuReference;
        existing.ReadType = project.ReadType;
        existing.Notes = project.Notes;
        existing.Tags = project.Tags;
        existing.UpdatedAt = project.UpdatedAt;

        foreach (var version in project.Versions.Where(version =>
            existing.Versions.All(savedVersion => savedVersion.Id != version.Id)))
        {
            dbContext.EcuProjectFileVersions.Add(new EcuProjectFileVersion
            {
                Id = version.Id,
                ProjectId = existing.Id,
                EcuFileId = version.EcuFileId,
                Kind = version.Kind,
                VersionNumber = version.VersionNumber,
                Label = version.Label,
                IsOriginalProtected = version.IsOriginalProtected,
                Notes = version.Notes,
                CreatedAt = version.CreatedAt
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EcuProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        return await dbContext.EcuProjects
            .Include(project => project.Versions)
            .AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EcuProject>> ListByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        var projects = await dbContext.EcuProjects
            .Include(project => project.Versions)
            .Where(project => project.VehicleId == vehicleId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return projects
            .OrderByDescending(project => project.UpdatedAt)
            .ToList();
    }
}
