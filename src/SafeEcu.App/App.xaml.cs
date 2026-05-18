using System.Windows;
using System.Windows.Threading;
using SafeEcu.Application.Common;
using SafeEcu.Infrastructure.Logging;

namespace SafeEcu.App;

public partial class App : System.Windows.Application
{
    private IAppLogger? _logger;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        var configuration = new AppConfiguration();
        _logger = new FileAppLogger(configuration.LogDirectory);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _logger.Information("Application starting.");

        var mainWindow = new MainWindow(_logger, configuration);
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("Unhandled UI error.", e.Exception);
        MessageBox.Show(
            "Ocorreu um erro inesperado. A aplicacao registou detalhes no log local.",
            "Erro",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.Error("Unhandled application error.", exception);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Information("Application closing.");
        base.OnExit(e);
    }
}
