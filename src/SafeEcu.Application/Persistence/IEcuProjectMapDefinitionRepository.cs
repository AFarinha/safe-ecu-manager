using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Persistence;

public interface IEcuProjectMapDefinitionRepository
{
    Task AddAsync(EcuProjectMapDefinition definition, CancellationToken cancellationToken = default);

    Task<EcuProjectMapDefinition?> GetByProjectAndMapIdAsync(
        Guid projectId,
        string mapId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EcuProjectMapDefinition>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
