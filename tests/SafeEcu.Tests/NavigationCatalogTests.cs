using SafeEcu.Application.Navigation;
using SafeEcu.Domain.Application;

namespace SafeEcu.Tests;

public sealed class NavigationCatalogTests
{
    [Fact]
    public void Sections_include_all_phase_one_navigation_areas()
    {
        var areas = NavigationCatalog.Sections.Select(section => section.Area).ToArray();

        Assert.Contains(ApplicationArea.Dashboard, areas);
        Assert.Contains(ApplicationArea.Vehicles, areas);
        Assert.Contains(ApplicationArea.Ecus, areas);
        Assert.Contains(ApplicationArea.EcuFiles, areas);
        Assert.Contains(ApplicationArea.Programmers, areas);
        Assert.Contains(ApplicationArea.Comparison, areas);
        Assert.Contains(ApplicationArea.Reports, areas);
        Assert.Contains(ApplicationArea.Audit, areas);
        Assert.Contains(ApplicationArea.Settings, areas);
    }

    [Fact]
    public void Phase_eight_sections_are_marked_implemented()
    {
        var implemented = NavigationCatalog.Sections
            .Where(section => section.IsImplemented)
            .Select(section => section.Area)
            .ToArray();

        Assert.Equal(
            [
                ApplicationArea.Dashboard,
                ApplicationArea.Vehicles,
                ApplicationArea.EcuFiles,
                ApplicationArea.Programmers,
                ApplicationArea.Comparison,
                ApplicationArea.Reports
            ],
            implemented);
    }

    [Fact]
    public void Sections_use_localization_keys_instead_of_display_text()
    {
        Assert.All(NavigationCatalog.Sections, section =>
        {
            Assert.StartsWith("Nav.", section.TitleKey);
            Assert.StartsWith("Nav.", section.DescriptionKey);
        });
    }
}
