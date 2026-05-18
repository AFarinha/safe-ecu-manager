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
    public void Only_dashboard_is_marked_implemented_in_phase_one()
    {
        var implemented = NavigationCatalog.Sections
            .Where(section => section.IsImplemented)
            .Select(section => section.Area)
            .ToArray();

        Assert.Equal([ApplicationArea.Dashboard], implemented);
    }
}
