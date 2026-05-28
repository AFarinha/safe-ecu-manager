using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuIdentificationService
{
    public EcuIdentificationResult Evaluate(EcuInfo ecuInfo)
    {
        var evidence = new List<string>();
        var missing = new List<string>();

        AddEvidence(ecuInfo.Manufacturer, "ECU manufacturer", evidence, missing);
        AddEvidence(ecuInfo.EcuFamily, "ECU family", evidence, missing);
        AddEvidence(ecuInfo.HardwareReference, "hardware reference", evidence, missing);
        AddEvidence(ecuInfo.SoftwareReference, "software reference", evidence, missing);
        AddEvidence(ecuInfo.SoftwareVersion, "software version", evidence, missing);
        AddEvidence(ecuInfo.Protocol, "protocol", evidence, missing);

        var confidence = evidence.Count switch
        {
            >= 5 => EcuIdentificationConfidence.High,
            >= 3 => EcuIdentificationConfidence.Medium,
            >= 1 => EcuIdentificationConfidence.Low,
            _ => EcuIdentificationConfidence.Unknown
        };

        var supportStatus = confidence == EcuIdentificationConfidence.Unknown
            ? SupportStatus.Unknown
            : ecuInfo.SupportStatus;

        return new EcuIdentificationResult(confidence, supportStatus, evidence, missing);
    }

    private static void AddEvidence(
        string? value,
        string label,
        ICollection<string> evidence,
        ICollection<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            missing.Add(label);
            return;
        }

        evidence.Add(label);
    }
}
