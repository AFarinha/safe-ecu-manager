using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.Persistence;
using SafeEcu.Application.Safety;
using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;

namespace SafeEcu.Application.PercentageIntent;

public sealed class PercentageIntentEngine
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IEcuInfoRepository _ecuInfoRepository;
    private readonly PercentageIntentValidationService _percentageValidationService;
    private readonly EcuIdentificationService _ecuIdentificationService;

    public PercentageIntentEngine(
        IVehicleRepository vehicleRepository,
        IEcuInfoRepository ecuInfoRepository,
        PercentageIntentValidationService percentageValidationService,
        EcuIdentificationService ecuIdentificationService)
    {
        _vehicleRepository = vehicleRepository;
        _ecuInfoRepository = ecuInfoRepository;
        _percentageValidationService = percentageValidationService;
        _ecuIdentificationService = ecuIdentificationService;
    }

    public async Task<PercentageIntentResult> EvaluateAsync(
        PercentageIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var blockReasons = new List<string>();

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            blockReasons.Add("Vehicle was not found.");
        }

        var ecuInfo = await _ecuInfoRepository.GetByIdAsync(request.EcuInfoId, cancellationToken);
        if (ecuInfo is null)
        {
            blockReasons.Add("ECU/EDU record was not found.");
        }
        else if (ecuInfo.VehicleId != request.VehicleId)
        {
            blockReasons.Add("ECU/EDU record does not belong to the selected vehicle.");
        }

        var percentageValidation = _percentageValidationService.Validate(
            new PercentageIntentValidationRequest(
                request.CalibrationProfileKey,
                request.RequestedPercentage));

        messages.AddRange(percentageValidation.Messages);
        blockReasons.AddRange(percentageValidation.BlockReasons);

        if (ecuInfo is not null)
        {
            var identification = _ecuIdentificationService.Evaluate(ecuInfo);
            if (identification.Confidence != EcuIdentificationConfidence.High)
            {
                blockReasons.Add("ECU/EDU identification confidence is not high enough for calibration intent.");
            }

            if (ecuInfo.SupportStatus is not SupportStatus.CalibrationPreview and not SupportStatus.Verified)
            {
                blockReasons.Add($"ECU/EDU support status is {ecuInfo.SupportStatus}; calibration intent is blocked.");
            }
        }

        if (percentageValidation.ProfileKey != "diagnostic-only")
        {
            blockReasons.Add("Technical map conversion rules are not implemented. No calibration changes can be generated.");
            blockReasons.Add("Checksum recalculation is not supported for calibration output.");
            blockReasons.Add("Original backup verification is required before any future calibration workflow.");
        }

        var changeSet = new CalibrationChangeSet(
            percentageValidation.ProfileKey,
            request.RequestedPercentage,
            percentageValidation.ProfileKey == "diagnostic-only"
                ? ["No ECU data changes are planned."]
                : ["Blocked before technical map conversion."],
            CreatesModifiedFile: false);

        var validationResult = blockReasons.Count == 0
            ? new CalibrationValidationResult(
                true,
                CalibrationSafetyStatus.Safe,
                messages,
                [],
                [],
                [],
                [])
            : new CalibrationValidationResult(
                false,
                CalibrationSafetyStatus.Blocked,
                messages,
                [],
                blockReasons,
                blockReasons,
                []);

        return new PercentageIntentResult(
            validationResult.IsAllowed,
            changeSet,
            validationResult,
            new SafetyReport(messages, blockReasons));
    }
}
