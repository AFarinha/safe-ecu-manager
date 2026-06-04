using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Projects;

public sealed class EcuProjectMapDefinitionService
{
    private readonly IEcuProjectRepository _projectRepository;
    private readonly IEcuProjectMapDefinitionRepository _mapDefinitionRepository;
    private readonly IAppLogger _logger;

    public EcuProjectMapDefinitionService(
        IEcuProjectRepository projectRepository,
        IEcuProjectMapDefinitionRepository mapDefinitionRepository,
        IAppLogger logger)
    {
        _projectRepository = projectRepository;
        _mapDefinitionRepository = mapDefinitionRepository;
        _logger = logger;
    }

    public async Task<OperationResult<EcuProjectMapDefinition>> CreateAsync(
        EcuProjectMapDefinitionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = await ValidateAsync(request, cancellationToken);
        if (blockReasons.Count > 0)
        {
            return OperationResult<EcuProjectMapDefinition>.Failure(string.Join(Environment.NewLine, blockReasons));
        }

        var now = DateTimeOffset.UtcNow;
        var definition = new EcuProjectMapDefinition
        {
            ProjectId = request.ProjectId,
            MapId = request.MapId.Trim(),
            DisplayName = request.DisplayName.Trim(),
            ParameterName = request.ParameterName.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Unknown" : request.Category.Trim(),
            StartOffset = request.StartOffset,
            Length = request.Length,
            RowCount = request.RowCount,
            ColumnCount = request.ColumnCount,
            DataType = request.DataType.Trim(),
            Endianess = request.Endianess.Trim(),
            Factor = request.Factor,
            OffsetCorrection = request.OffsetCorrection,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "raw" : request.Unit.Trim(),
            XAxisSourceOffset = request.XAxisSourceOffset,
            XAxisLength = request.XAxisLength,
            XAxisUnit = NormalizeOptional(request.XAxisUnit),
            YAxisSourceOffset = request.YAxisSourceOffset,
            YAxisLength = request.YAxisLength,
            YAxisUnit = NormalizeOptional(request.YAxisUnit),
            MinimumValue = request.MinimumValue,
            MaximumValue = request.MaximumValue,
            IsEmissionsRelated = request.IsEmissionsRelated,
            RequiredMapIds = NormalizeOptional(request.RequiredMapIds) ?? string.Empty,
            ProfileId = NormalizeOptional(request.ProfileId) ?? string.Empty,
            SupportStatus = NormalizeOptional(request.SupportStatus) ?? "Unknown",
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _mapDefinitionRepository.AddAsync(definition, cancellationToken);
        _logger.Information($"ECU project map definition created: {definition.Id}, map {definition.MapId}.");

        return OperationResult<EcuProjectMapDefinition>.Success(definition);
    }

    public Task<IReadOnlyList<EcuProjectMapDefinition>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default) =>
        _mapDefinitionRepository.ListByProjectAsync(projectId, cancellationToken);

    private async Task<List<string>> ValidateAsync(
        EcuProjectMapDefinitionCreateRequest request,
        CancellationToken cancellationToken)
    {
        var blockReasons = new List<string>();
        if (request.ProjectId == Guid.Empty)
        {
            blockReasons.Add("Map definitions must be associated with an ECU project.");
        }
        else if (await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken) is null)
        {
            blockReasons.Add("The ECU project was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.MapId))
        {
            blockReasons.Add("Map id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            blockReasons.Add("Map display name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ParameterName))
        {
            blockReasons.Add("Map parameter name is required.");
        }

        if (request.StartOffset < 0 || request.Length <= 0)
        {
            blockReasons.Add("Map address range is invalid.");
        }

        if (request.RowCount is <= 0 || request.ColumnCount is <= 0)
        {
            blockReasons.Add("Map dimensions must be positive when provided.");
        }

        if (request.Factor <= 0)
        {
            blockReasons.Add("Map factor must be greater than zero.");
        }

        if (request.MinimumValue is not null && request.MaximumValue is not null && request.MinimumValue > request.MaximumValue)
        {
            blockReasons.Add("Map min/max values are invalid.");
        }

        if (request.IsEmissionsRelated)
        {
            blockReasons.Add("Emissions-related map definitions are blocked in this application.");
        }

        if (!string.IsNullOrWhiteSpace(request.MapId)
            && await _mapDefinitionRepository.GetByProjectAndMapIdAsync(request.ProjectId, request.MapId, cancellationToken) is not null)
        {
            blockReasons.Add("A map definition with the same id already exists in this project.");
        }

        return blockReasons;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
