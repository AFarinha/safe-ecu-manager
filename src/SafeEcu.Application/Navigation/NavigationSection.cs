using SafeEcu.Domain.Application;

namespace SafeEcu.Application.Navigation;

public sealed record NavigationSection(
    ApplicationArea Area,
    string TitleKey,
    string DescriptionKey,
    bool IsImplemented);
