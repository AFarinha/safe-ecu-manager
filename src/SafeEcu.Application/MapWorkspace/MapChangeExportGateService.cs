namespace SafeEcu.Application.MapWorkspace;

public sealed class MapChangeExportGateService
{
    public MapChangeExportGateResult Evaluate(MapChangeExportGateRequest request)
    {
        var messages = new List<string>();
        var blockReasons = new List<string>();

        messages.AddRange(request.EvidenceResult.Messages);
        messages.AddRange(request.ChangePlan.Messages);
        messages.AddRange(request.ChecksumSupport.Messages);
        messages.AddRange(request.SafetyValidation.Messages);

        if (!request.EvidenceResult.IsComplete)
        {
            blockReasons.AddRange(request.EvidenceResult.MissingEvidence.Select(item => $"Missing evidence: {item}."));
            blockReasons.AddRange(request.EvidenceResult.BlockReasons);
        }

        if (!request.ChangePlan.IsAllowed)
        {
            blockReasons.AddRange(request.ChangePlan.BlockReasons);
        }

        if (!request.ChecksumSupport.CanExportCalibrationOutput)
        {
            blockReasons.AddRange(request.ChecksumSupport.BlockReasons);
            blockReasons.Add("Checksum support is required before exporting a modified calibration file.");
        }

        if (!request.SafetyValidation.IsAllowed)
        {
            blockReasons.AddRange(request.SafetyValidation.BlockReasons);
            blockReasons.AddRange(request.SafetyValidation.Errors);
        }

        return new MapChangeExportGateResult(
            blockReasons.Count == 0,
            blockReasons.Count == 0
                ? ["All export gates passed."]
                : messages,
            blockReasons.Distinct().ToArray());
    }
}
