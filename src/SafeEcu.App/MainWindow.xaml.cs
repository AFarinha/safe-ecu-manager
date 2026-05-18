using System.Windows;
using System.Windows.Controls;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.Navigation;
using SafeEcu.Domain.Application;

namespace SafeEcu.App;

public partial class MainWindow : Window
{
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _configuration;
    private readonly ITextLocalizer _localizer;
    private ApplicationArea _currentArea = ApplicationArea.Dashboard;

    public MainWindow(IAppLogger logger, AppConfiguration configuration, ITextLocalizer localizer)
    {
        _logger = logger;
        _configuration = configuration;
        _localizer = localizer;

        InitializeComponent();

        LanguageSelector.ItemsSource = _localizer.SupportedLanguages;
        LanguageSelector.SelectedValue = _localizer.CurrentLanguageCode;

        RefreshLocalizedText();
        ShowSection(ApplicationArea.Dashboard);
    }

    private void OnNavigationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ApplicationArea area })
        {
            ShowSection(area);
        }
    }

    private void ShowSection(ApplicationArea area)
    {
        _currentArea = area;
        var section = NavigationCatalog.Sections.First(item => item.Area == area);

        SectionTitle.Text = _localizer.Text(section.TitleKey);
        SectionDescription.Text = _localizer.Text(section.DescriptionKey);
        SectionStatus.Text = section.IsImplemented
            ? _localizer.Text("Status.AvailableNow")
            : _localizer.Text("Status.FuturePhase");
        SectionBody.Text = BuildBody(section);

        _logger.Information($"Navigation selected: {section.Area}.");
    }

    private string BuildBody(NavigationSection section)
    {
        if (section.Area == ApplicationArea.Dashboard)
        {
            return _localizer.Text("Dashboard.Body");
        }

        return _localizer.Text("Section.FutureBody");
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageSelector.SelectedValue is not string languageCode)
        {
            return;
        }

        _localizer.SetLanguage(languageCode);
        RefreshLocalizedText();
        ShowSection(_currentArea);
        _logger.Information($"Language selected: {_localizer.CurrentLanguageCode}.");
    }

    private void RefreshLocalizedText()
    {
        Title = _localizer.Text("App.Title");
        LanguageLabel.Text = _localizer.Text("Language.Label");
        SafeModeFooter.Text = _localizer.Text("Footer.SafeMode");
        NavigationItems.ItemsSource = NavigationCatalog.Sections
            .Select(section => new
            {
                section.Area,
                Title = _localizer.Text(section.TitleKey)
            })
            .ToArray();
    }
}
