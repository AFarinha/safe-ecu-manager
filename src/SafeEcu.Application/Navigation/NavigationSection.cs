using SafeEcu.Domain.Application;

namespace SafeEcu.Application.Navigation;

public sealed record NavigationSection(
    ApplicationArea Area,
    string Title,
    string Description,
    bool IsImplemented);
