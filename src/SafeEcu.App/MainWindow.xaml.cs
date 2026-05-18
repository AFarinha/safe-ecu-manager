using System.Windows;
using System.Windows.Controls;
using SafeEcu.Application.Common;
using SafeEcu.Application.Navigation;
using SafeEcu.Domain.Application;

namespace SafeEcu.App;

public partial class MainWindow : Window
{
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _configuration;

    public MainWindow(IAppLogger logger, AppConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        InitializeComponent();

        NavigationItems.ItemsSource = NavigationCatalog.Sections;
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
        var section = NavigationCatalog.Sections.First(item => item.Area == area);

        SectionTitle.Text = section.Title;
        SectionDescription.Text = section.Description;
        SectionStatus.Text = section.IsImplemented ? "Disponivel nesta fase" : "Preparado para fase futura";
        SectionBody.Text = BuildBody(section);

        _logger.Information($"Navigation selected: {section.Area}.");
    }

    private string BuildBody(NavigationSection section)
    {
        if (section.Area == ApplicationArea.Dashboard)
        {
            return
                $"{_configuration.ApplicationName} esta na Fase 1: base da aplicacao.\n\n" +
                "Esta entrega cria a shell desktop, a navegacao principal, logging local, tratamento global de erros e configuracao base.\n\n" +
                "Funcionalidades perigosas permanecem desativadas: escrita ECU, flashing, controlo direto do Galletto 1260, alteracao real de mapas e qualquer bypass tecnico.";
        }

        return
            "Esta seccao ja existe na navegacao para estabilizar a estrutura da aplicacao, mas a funcionalidade sera implementada numa fase propria.\n\n" +
            "Enquanto nao existir suporte validado, o comportamento esperado e bloquear ou marcar como NotSupported.";
    }
}
