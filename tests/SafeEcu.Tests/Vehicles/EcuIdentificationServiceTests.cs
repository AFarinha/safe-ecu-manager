using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Tests.Vehicles;

public sealed class EcuIdentificationServiceTests
{
    [Fact]
    public void Unknown_fields_result_in_unknown_confidence_and_unknown_support()
    {
        var result = new EcuIdentificationService().Evaluate(new EcuInfo
        {
            SupportStatus = SupportStatus.Verified
        });

        Assert.Equal(EcuIdentificationConfidence.Unknown, result.Confidence);
        Assert.Equal(SupportStatus.Unknown, result.SupportStatus);
        Assert.NotEmpty(result.MissingEvidence);
    }

    [Fact]
    public void Complete_identification_result_is_high_confidence()
    {
        var result = new EcuIdentificationService().Evaluate(new EcuInfo
        {
            Manufacturer = "Delphi",
            EcuFamily = "Unknown Isuzu/Delco/Delphi",
            HardwareReference = "HW123",
            SoftwareReference = "SW456",
            SoftwareVersion = "1.0",
            Protocol = "K-Line",
            SupportStatus = SupportStatus.FileManagement
        });

        Assert.Equal(EcuIdentificationConfidence.High, result.Confidence);
        Assert.Equal(SupportStatus.FileManagement, result.SupportStatus);
        Assert.Empty(result.MissingEvidence);
    }
}
