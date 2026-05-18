using System.Windows;
using System.Windows.Threading;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.Vehicles;
using SafeEcu.Infrastructure.Logging;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;

namespace SafeEcu.App;

public partial class App : System.Windows.Application
{
    private IAppLogger? _logger;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var configuration = new AppConfiguration();
        var localizer = new InMemoryTextLocalizer(configuration.DefaultLanguage);
        _logger = new FileAppLogger(configuration.LogDirectory);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        _logger.Information("Application starting.");

        var connectionString = SqliteConnectionStringFactory.Create(configuration.DataDirectory);
        var dbContextFactory = new SafeEcuDbContextFactory(connectionString);
        var persistenceInitializer = new SqlitePersistenceInitializer(dbContextFactory, _logger);
        var persistenceResult = await persistenceInitializer.InitializeAsync();

        if (!persistenceResult.IsSuccess)
        {
            MessageBox.Show(
                persistenceResult.ErrorMessage,
                localizer.Text("App.DatabaseWarningTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        var vehicleRepository = new VehicleRepository(dbContextFactory);
        var vehicleService = new VehicleService(vehicleRepository, _logger);
        var vehicleProfileCatalog = new VehicleProfileCatalog();

        var mainWindow = new MainWindow(_logger, configuration, localizer, vehicleService, vehicleProfileCatalog);
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error("Unhandled UI error.", e.Exception);
        var localizer = new InMemoryTextLocalizer();
        MessageBox.Show(
            localizer.Text("App.UnhandledError"),
            localizer.Text("App.ErrorTitle"),
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
