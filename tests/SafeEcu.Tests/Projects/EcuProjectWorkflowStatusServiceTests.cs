using SafeEcu.Application.Projects;
using SafeEcu.Domain.Projects;

namespace SafeEcu.Tests.Projects;

public sealed class EcuProjectWorkflowStatusServiceTests
{
    [Fact]
    public void Evaluate_returns_draft_when_original_is_not_protected()
    {
        var service = new EcuProjectWorkflowStatusService();
        var project = new EcuProject { Name = "Draft" };

        var state = service.Evaluate(project);

        Assert.Equal(EcuProjectWorkflowStatus.Draft, state.Status);
        Assert.Contains(state.MissingSteps, step => step.Contains("original", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(state.BlockReasons);
    }

    [Fact]
    public void Evaluate_returns_original_protected_when_no_maps_are_defined()
    {
        var service = new EcuProjectWorkflowStatusService();
        var project = CreateProjectWithProtectedOriginal();

        var state = service.Evaluate(project);

        Assert.Equal(EcuProjectWorkflowStatus.OriginalProtected, state.Status);
        Assert.Contains(state.CompletedSteps, step => step.Contains("Protected original", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Define", state.NextStep, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_returns_maps_defined_when_project_has_map_definitions()
    {
        var service = new EcuProjectWorkflowStatusService();
        var project = CreateProjectWithProtectedOriginal();
        project.MapDefinitions.Add(new EcuProjectMapDefinition
        {
            MapId = "manual-map",
            DisplayName = "Manual map",
            ParameterName = "Unknown",
            StartOffset = 0,
            Length = 16
        });

        var state = service.Evaluate(project);

        Assert.Equal(EcuProjectWorkflowStatus.MapsDefined, state.Status);
        Assert.Contains(state.CompletedSteps, step => step.Contains("Map definitions", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(state.BlockReasons, reason => reason.Contains("Export remains blocked", StringComparison.OrdinalIgnoreCase));
    }

    private static EcuProject CreateProjectWithProtectedOriginal()
    {
        var originalFileId = Guid.NewGuid();
        return new EcuProject
        {
            Name = "Protected",
            OriginalFileId = originalFileId,
            Versions =
            [
                new EcuProjectFileVersion
                {
                    EcuFileId = originalFileId,
                    Kind = EcuProjectFileVersionKind.Original,
                    IsOriginalProtected = true,
                    VersionNumber = 1,
                    Label = "Original"
                }
            ]
        };
    }
}
