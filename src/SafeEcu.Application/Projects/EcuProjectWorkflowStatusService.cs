using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Projects;

public sealed class EcuProjectWorkflowStatusService
{
    public EcuProjectWorkflowState Evaluate(EcuProject project)
    {
        var completed = new List<string>();
        var missing = new List<string>();
        var blocks = new List<string>();

        var hasProtectedOriginal = project.OriginalFileId != Guid.Empty
            && project.Versions.Any(version =>
                version.Kind == EcuProjectFileVersionKind.Original && version.IsOriginalProtected);
        if (hasProtectedOriginal)
        {
            completed.Add("Protected original file");
        }
        else
        {
            missing.Add("Protected original file");
            blocks.Add("Import and protect an original ECU file before defining maps.");
        }

        if (project.MapDefinitions.Count > 0)
        {
            completed.Add("Map definitions");
        }
        else
        {
            missing.Add("Map definitions");
        }

        var modifiedVersions = project.Versions.Count(version => version.Kind == EcuProjectFileVersionKind.Modified);
        if (modifiedVersions > 0)
        {
            completed.Add("Modified version");
        }
        else
        {
            missing.Add("Modified version");
        }

        if (!hasProtectedOriginal)
        {
            return new EcuProjectWorkflowState(
                EcuProjectWorkflowStatus.Draft,
                "Draft",
                "Import or select an Original ECU file.",
                completed,
                missing,
                blocks);
        }

        if (project.MapDefinitions.Count == 0)
        {
            return new EcuProjectWorkflowState(
                EcuProjectWorkflowStatus.OriginalProtected,
                "Original protected",
                "Define at least one map in Map Workspace.",
                completed,
                missing,
                blocks);
        }

        blocks.Add("Export remains blocked until edit preview, checksum support, safety validation and explicit confirmation pass.");
        return new EcuProjectWorkflowState(
            modifiedVersions > 0 ? EcuProjectWorkflowStatus.ValidationRequired : EcuProjectWorkflowStatus.MapsDefined,
            modifiedVersions > 0 ? "Validation required" : "Maps defined",
            modifiedVersions > 0 ? "Run validation and generate report before export." : "Preview/edit defined maps, then validate.",
            completed,
            missing,
            blocks);
    }
}
