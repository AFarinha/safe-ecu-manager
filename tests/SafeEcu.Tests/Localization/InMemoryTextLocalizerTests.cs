using SafeEcu.Application.Localization;

namespace SafeEcu.Tests.Localization;

public sealed class InMemoryTextLocalizerTests
{
    [Fact]
    public void Defaults_to_english()
    {
        var localizer = new InMemoryTextLocalizer();

        Assert.Equal("en", localizer.CurrentLanguageCode);
        Assert.Equal("Vehicles", localizer.Text("Nav.Vehicles.Title"));
        Assert.Contains("blocked", localizer.Text("CalibrationPreview.SafeBody"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Supports_portuguese()
    {
        var localizer = new InMemoryTextLocalizer();

        localizer.SetLanguage("pt");

        Assert.Equal("pt", localizer.CurrentLanguageCode);
        Assert.Equal("Veiculos", localizer.Text("Nav.Vehicles.Title"));
    }

    [Fact]
    public void Unknown_language_falls_back_to_english()
    {
        var localizer = new InMemoryTextLocalizer();

        localizer.SetLanguage("unknown");

        Assert.Equal("en", localizer.CurrentLanguageCode);
        Assert.Equal("ECU Files", localizer.Text("Nav.EcuFiles.Title"));
    }
}
