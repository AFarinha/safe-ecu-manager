using SafeEcu.Application.Persistence;

namespace SafeEcu.Application.Projects;

public sealed class EcuProjectMapExportGateService
{
    private readonly IEcuProjectRepository _projectRepository;

    public EcuProjectMapExportGateService(IEcuProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<EcuProjectMapExportGateResult> EvaluateAsync(
        EcuProjectMapExportGateRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = new List<string>();
        if (request.ProjectId == Guid.Empty)
        {
            blockReasons.Add("ECU project is required before exporting a modified file.");
        }
        else if (await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken) is null)
        {
            blockReasons.Add("ECU project was not found.");
        }

        if (!request.EditPreview.IsAllowed)
        {
            blockReasons.Add("A valid controlled edit preview is required before export.");
        }

        if (!IsSupportedForExport(request.SupportStatus))
        {
            blockReasons.Add("Project/map support status is not Verified; export is blocked.");
        }

        if (!request.ChecksumSupported)
        {
            blockReasons.Add("Checksum algorithm is not supported for this ECU/software; export is blocked.");
        }

        if (!request.ChecksumWillBeRecalculated)
        {
            blockReasons.Add("Checksum must be recalculated before exporting a modified file.");
        }

        if (!request.ExplicitUserConfirmation)
        {
            blockReasons.Add("Explicit user confirmation is required before exporting a modified file.");
        }

        if (blockReasons.Count > 0)
        {
            return new EcuProjectMapExportGateResult(false, [], blockReasons);
        }

        return new EcuProjectMapExportGateResult(
            true,
            [
                "Export gate passed for a controlled file output workflow.",
                "This does not allow ECU writing, flashing or hardware control."
            ],
            []);
    }

    private static bool IsSupportedForExport(string supportStatus) =>
        string.Equals(supportStatus, "Verified", StringComparison.OrdinalIgnoreCase);
}
