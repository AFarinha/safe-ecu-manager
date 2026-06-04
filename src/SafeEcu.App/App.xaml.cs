using System.Windows;
using System.Windows.Threading;
using SafeEcu.Calibration;
using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.MapWorkspace;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Programmers;
using SafeEcu.Application.Projects;
using SafeEcu.Application.Vehicles;
using SafeEcu.Infrastructure.Files;
using SafeEcu.Infrastructure.Logging;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Infrastructure.Reports;
using SafeEcu.Programmers;

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
        var ecuInfoRepository = new EcuInfoRepository(dbContextFactory);
        var ecuFileRepository = new EcuFileRepository(dbContextFactory);
        var vehicleService = new VehicleService(vehicleRepository, _logger);
        var ecuIdentificationService = new EcuIdentificationService();
        var ecuInfoService = new EcuInfoService(ecuInfoRepository, _logger, ecuIdentificationService);
        var ecuFileService = new EcuFileService(ecuFileRepository, _logger);
        var renaultMegane3BaselineSeeder = new RenaultMegane3BaselineSeeder(
            vehicleRepository,
            ecuInfoRepository,
            _logger);
        await renaultMegane3BaselineSeeder.SeedIfEmptyAsync();
        var ecuFileValidationService = new EcuFileValidationService(ecuFileRepository);
        var ecuFileImportService = new EcuFileImportService(
            ecuInfoRepository,
            ecuFileService,
            ecuFileValidationService,
            new Sha256FileHashService(),
            _logger);
        var comparisonRepository = new CalibrationComparisonRepository(dbContextFactory);
        var ecuProjectRepository = new EcuProjectRepository(dbContextFactory);
        var ecuProjectMapDefinitionRepository = new EcuProjectMapDefinitionRepository(dbContextFactory);
        var binaryComparisonService = new BinaryComparisonService(ecuFileRepository, comparisonRepository, _logger);
        var mapWorkspacePreviewService = new MapWorkspacePreviewService();
        var ecuProjectService = new EcuProjectService(ecuProjectRepository, ecuFileRepository, _logger);
        var ecuProjectMapDefinitionService = new EcuProjectMapDefinitionService(
            ecuProjectRepository,
            ecuProjectMapDefinitionRepository,
            _logger);
        var ecuProjectMapSnapshotService = new EcuProjectMapSnapshotService(ecuProjectMapDefinitionRepository);
        var ecuProjectMapExportGateService = new EcuProjectMapExportGateService(ecuProjectRepository);
        var calibrationProfileCatalog = new CalibrationProfileCatalog();
        var percentageIntentEngine = new PercentageIntentEngine(
            vehicleRepository,
            ecuInfoRepository,
            new PercentageIntentValidationService(calibrationProfileCatalog),
            ecuIdentificationService);
        var reportService = new HtmlTechnicalReportService(dbContextFactory, _logger);
        var vehicleProfileCatalog = new VehicleProfileCatalog();
        var programmerCapabilityService = new ProgrammerCapabilityService(ProgrammerAdapterCatalog.CreateDefaultAdapters());

        var mainWindow = new MainWindow(
            _logger,
            configuration,
            localizer,
            vehicleService,
            ecuInfoService,
            vehicleProfileCatalog,
            programmerCapabilityService,
            ecuFileImportService,
            ecuFileService,
            binaryComparisonService,
            mapWorkspacePreviewService,
            ecuProjectService,
            ecuProjectMapDefinitionService,
            ecuProjectMapSnapshotService,
            ecuProjectMapExportGateService,
            percentageIntentEngine,
            calibrationProfileCatalog,
            reportService);
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
