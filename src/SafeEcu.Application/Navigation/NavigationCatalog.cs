using SafeEcu.Domain.Application;

namespace SafeEcu.Application.Navigation;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationSection> Sections { get; } =
    [
        new(ApplicationArea.Dashboard, "Nav.Dashboard.Title", "Nav.Dashboard.Description", true),
        new(ApplicationArea.Vehicles, "Nav.Vehicles.Title", "Nav.Vehicles.Description", true),
        new(ApplicationArea.Ecus, "Nav.Ecus.Title", "Nav.Ecus.Description", true),
        new(ApplicationArea.EcuFiles, "Nav.EcuFiles.Title", "Nav.EcuFiles.Description", true),
        new(ApplicationArea.Programmers, "Nav.Programmers.Title", "Nav.Programmers.Description", true),
        new(ApplicationArea.Comparison, "Nav.Comparison.Title", "Nav.Comparison.Description", true),
        new(ApplicationArea.CalibrationPreview, "Nav.CalibrationPreview.Title", "Nav.CalibrationPreview.Description", false),
        new(ApplicationArea.Reports, "Nav.Reports.Title", "Nav.Reports.Description", true),
        new(ApplicationArea.Audit, "Nav.Audit.Title", "Nav.Audit.Description", false),
        new(ApplicationArea.Settings, "Nav.Settings.Title", "Nav.Settings.Description", false)
    ];
}
