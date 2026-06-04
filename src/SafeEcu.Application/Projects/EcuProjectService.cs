using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Projects;

public sealed class EcuProjectService
{
    private readonly IEcuProjectRepository _projectRepository;
    private readonly IEcuFileRepository _ecuFileRepository;
    private readonly IAppLogger _logger;

    public EcuProjectService(
        IEcuProjectRepository projectRepository,
        IEcuFileRepository ecuFileRepository,
        IAppLogger logger)
    {
        _projectRepository = projectRepository;
        _ecuFileRepository = ecuFileRepository;
        _logger = logger;
    }

    public async Task<OperationResult<EcuProject>> CreateAsync(
        EcuProjectCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.VehicleId == Guid.Empty)
        {
            return OperationResult<EcuProject>.Failure("The ECU project must be associated with a vehicle.");
        }

        if (request.OriginalFileId == Guid.Empty)
        {
            return OperationResult<EcuProject>.Failure("The ECU project requires an original ECU file.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return OperationResult<EcuProject>.Failure("The ECU project name is required.");
        }

        var originalFile = await _ecuFileRepository.GetByIdAsync(request.OriginalFileId, cancellationToken);
        if (originalFile is null)
        {
            return OperationResult<EcuProject>.Failure("The selected original ECU file was not found.");
        }

        if (originalFile.VehicleId != request.VehicleId)
        {
            return OperationResult<EcuProject>.Failure("The selected original ECU file belongs to a different vehicle.");
        }

        if (originalFile.FileType != EcuFileType.Original)
        {
            return OperationResult<EcuProject>.Failure("The protected project file must be registered as Original.");
        }

        var now = DateTimeOffset.UtcNow;
        var project = new EcuProject
        {
            VehicleId = request.VehicleId,
            EcuInfoId = originalFile.EcuInfoId,
            OriginalFileId = originalFile.Id,
            Name = request.Name.Trim(),
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Engine = request.Engine.Trim(),
            Year = request.Year,
            EcuType = request.EcuType.Trim(),
            EcuReference = NormalizeOptional(request.EcuReference),
            ReadType = request.ReadType.Trim(),
            Notes = NormalizeOptional(request.Notes),
            Tags = NormalizeOptional(request.Tags),
            CreatedAt = now,
            UpdatedAt = now,
            Versions =
            [
                new EcuProjectFileVersion
                {
                    EcuFileId = originalFile.Id,
                    Kind = EcuProjectFileVersionKind.Original,
                    VersionNumber = 1,
                    Label = "Original protected backup",
                    IsOriginalProtected = true,
                    Notes = "Automatically created from the imported original ECU file.",
                    CreatedAt = now
                }
            ]
        };

        await _projectRepository.AddAsync(project, cancellationToken);
        _logger.Information($"ECU project created: {project.Id}.");

        return OperationResult<EcuProject>.Success(project);
    }

    public async Task<OperationResult<EcuProjectFileVersion>> AddVersionAsync(
        EcuProjectAddVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProjectId == Guid.Empty || request.EcuFileId == Guid.Empty)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("Project and ECU file are required.");
        }

        if (request.Kind == EcuProjectFileVersionKind.Original)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("Original project files are protected and cannot be added as editable versions.");
        }

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("The ECU project was not found.");
        }

        var ecuFile = await _ecuFileRepository.GetByIdAsync(request.EcuFileId, cancellationToken);
        if (ecuFile is null)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("The ECU file was not found.");
        }

        if (ecuFile.VehicleId != project.VehicleId)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("The ECU file belongs to a different vehicle.");
        }

        if (ecuFile.Id == project.OriginalFileId)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("The protected original file cannot be re-added as a modified version.");
        }

        var existing = project.Versions.FirstOrDefault(version => version.EcuFileId == ecuFile.Id);
        if (existing is not null)
        {
            return OperationResult<EcuProjectFileVersion>.Failure("This ECU file is already linked to the project.");
        }

        var nextVersionNumber = project.Versions.Count == 0
            ? 1
            : project.Versions.Max(version => version.VersionNumber) + 1;
        var version = new EcuProjectFileVersion
        {
            ProjectId = project.Id,
            EcuFileId = ecuFile.Id,
            Kind = request.Kind,
            VersionNumber = nextVersionNumber,
            Label = string.IsNullOrWhiteSpace(request.Label) ? $"Version {nextVersionNumber}" : request.Label.Trim(),
            IsOriginalProtected = false,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = DateTimeOffset.UtcNow
        };

        project.Versions.Add(version);
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await _projectRepository.UpdateAsync(project, cancellationToken);
        _logger.Information($"ECU project version added: {project.Id}, file {ecuFile.Id}.");

        return OperationResult<EcuProjectFileVersion>.Success(version);
    }

    public Task<EcuProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _projectRepository.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<EcuProject>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        _projectRepository.ListByVehicleAsync(vehicleId, cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
