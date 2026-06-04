using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using SafeEcu.Calibration;
using SafeEcu.Application.Calibrations;
using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.MapWorkspace;
using SafeEcu.Application.Navigation;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Programmers;
using SafeEcu.Application.Projects;
using SafeEcu.Application.Reports;
using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.App;

public partial class MainWindow : Window
{
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _configuration;
    private readonly ITextLocalizer _localizer;
    private readonly VehicleService _vehicleService;
    private readonly EcuInfoService _ecuInfoService;
    private readonly VehicleProfileCatalog _vehicleProfileCatalog;
    private readonly ProgrammerCapabilityService _programmerCapabilityService;
    private readonly EcuFileImportService _ecuFileImportService;
    private readonly EcuFileService _ecuFileService;
    private readonly BinaryComparisonService _binaryComparisonService;
    private readonly MapWorkspacePreviewService _mapWorkspacePreviewService;
    private readonly EcuProjectService _ecuProjectService;
    private readonly EcuProjectMapDefinitionService _ecuProjectMapDefinitionService;
    private readonly EcuProjectMapSnapshotService _ecuProjectMapSnapshotService;
    private readonly EcuProjectWorkflowStatusService _ecuProjectWorkflowStatusService = new();
    private readonly EcuProjectMapEditPreviewService _ecuProjectMapEditPreviewService = new();
    private readonly PercentageIntentEngine _percentageIntentEngine;
    private readonly CalibrationProfileCatalog _calibrationProfileCatalog;
    private readonly IReportService _reportService;
    private ApplicationArea _currentArea = ApplicationArea.Dashboard;
    private Guid? _selectedVehicleId;
    private Guid? _selectedEcuId;
    private bool _isLoadingVehicle;
    private bool _isLoadingEcu;

    public MainWindow(
        IAppLogger logger,
        AppConfiguration configuration,
        ITextLocalizer localizer,
        VehicleService vehicleService,
        EcuInfoService ecuInfoService,
        VehicleProfileCatalog vehicleProfileCatalog,
        ProgrammerCapabilityService programmerCapabilityService,
        EcuFileImportService ecuFileImportService,
        EcuFileService ecuFileService,
        BinaryComparisonService binaryComparisonService,
        MapWorkspacePreviewService mapWorkspacePreviewService,
        EcuProjectService ecuProjectService,
        EcuProjectMapDefinitionService ecuProjectMapDefinitionService,
        EcuProjectMapSnapshotService ecuProjectMapSnapshotService,
        PercentageIntentEngine percentageIntentEngine,
        CalibrationProfileCatalog calibrationProfileCatalog,
        IReportService reportService)
    {
        _logger = logger;
        _configuration = configuration;
        _localizer = localizer;
        _vehicleService = vehicleService;
        _ecuInfoService = ecuInfoService;
        _vehicleProfileCatalog = vehicleProfileCatalog;
        _programmerCapabilityService = programmerCapabilityService;
        _ecuFileImportService = ecuFileImportService;
        _ecuFileService = ecuFileService;
        _binaryComparisonService = binaryComparisonService;
        _mapWorkspacePreviewService = mapWorkspacePreviewService;
        _ecuProjectService = ecuProjectService;
        _ecuProjectMapDefinitionService = ecuProjectMapDefinitionService;
        _ecuProjectMapSnapshotService = ecuProjectMapSnapshotService;
        _percentageIntentEngine = percentageIntentEngine;
        _calibrationProfileCatalog = calibrationProfileCatalog;
        _reportService = reportService;

        InitializeComponent();

        VehicleFuelSelector.ItemsSource = Enum.GetValues<FuelType>();
        VehicleFuelSelector.SelectedItem = FuelType.Unknown;
        VehicleProfileSelector.ItemsSource = BuildProfileOptions();
        EcuSupportStatusSelector.ItemsSource = Enum.GetValues<SupportStatus>();
        EcuSupportStatusSelector.SelectedItem = SupportStatus.Unknown;
        EcuFileTypeSelector.ItemsSource = Enum.GetValues<EcuFileType>();
        EcuFileTypeSelector.SelectedItem = EcuFileType.Unknown;
        EcuFileReadMethodTextBox.Text = "ManualWorkflowOnly";
        EcuFileProgrammerTextBox.Text = "Galletto 1260 / EOBD Programmer 1260";

        LanguageSelector.ItemsSource = _localizer.SupportedLanguages;
        LanguageSelector.SelectedValue = _localizer.CurrentLanguageCode;
        MapWorkspaceProfileSelector.ItemsSource = BuildCalibrationProfileOptions();
        MapWorkspaceDataTypeSelector.ItemsSource = new[] { "UInt8", "Int8", "UInt16", "Int16", "UInt32", "Int32" };
        MapWorkspaceDataTypeSelector.SelectedItem = "UInt8";
        MapWorkspaceEditOperationSelector.ItemsSource = BuildMapEditOperationOptions();
        MapWorkspaceEditOperationSelector.SelectedItem = ((IReadOnlyList<MapEditOperationOption>)MapWorkspaceEditOperationSelector.ItemsSource)[0];

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
        var isProjects = area == ApplicationArea.Projects;
        var isVehicles = area == ApplicationArea.Vehicles;
        var isEcus = area == ApplicationArea.Ecus;
        var isEcuFiles = area == ApplicationArea.EcuFiles;
        var isComparison = area == ApplicationArea.Comparison;
        var isMapWorkspace = area == ApplicationArea.CalibrationPreview;
        var isReports = area == ApplicationArea.Reports;
        var isProgrammers = area == ApplicationArea.Programmers;
        GenericPanel.Visibility = isProjects || isVehicles || isEcus || isEcuFiles || isComparison || isMapWorkspace || isReports || isProgrammers ? Visibility.Collapsed : Visibility.Visible;
        ProjectsPanel.Visibility = isProjects ? Visibility.Visible : Visibility.Collapsed;
        VehiclesPanel.Visibility = isVehicles ? Visibility.Visible : Visibility.Collapsed;
        EcusPanel.Visibility = isEcus ? Visibility.Visible : Visibility.Collapsed;
        EcuFilesPanel.Visibility = isEcuFiles ? Visibility.Visible : Visibility.Collapsed;
        ComparisonPanel.Visibility = isComparison ? Visibility.Visible : Visibility.Collapsed;
        MapWorkspacePanel.Visibility = isMapWorkspace ? Visibility.Visible : Visibility.Collapsed;
        ReportsPanel.Visibility = isReports ? Visibility.Visible : Visibility.Collapsed;
        ProgrammersPanel.Visibility = isProgrammers ? Visibility.Visible : Visibility.Collapsed;

        if (isProjects)
        {
            _ = LoadProjectsAsync();
        }
        else if (isVehicles)
        {
            _ = LoadVehiclesAsync();
        }
        else if (isEcus)
        {
            _ = LoadEcuVehiclesAsync();
        }
        else if (isEcuFiles)
        {
            _ = LoadEcuFileVehiclesAsync();
        }
        else if (isComparison)
        {
            _ = LoadComparisonVehiclesAsync();
        }
        else if (isMapWorkspace)
        {
            _ = LoadMapWorkspaceVehiclesAsync();
        }
        else if (isReports)
        {
            _ = LoadReportComparisonsAsync();
        }
        else if (isProgrammers)
        {
            LoadProgrammerCapabilities();
        }

        _logger.Information($"Navigation selected: {section.Area}.");
    }

    private string BuildBody(NavigationSection section)
    {
        if (section.Area == ApplicationArea.Dashboard)
        {
            return _localizer.Text("Dashboard.Body");
        }

        if (section.Area == ApplicationArea.CalibrationPreview)
        {
            return _localizer.Text("CalibrationPreview.SafeBody");
        }

        return _localizer.Text("Section.FutureBody");
    }

    private async Task LoadProjectsAsync()
    {
        try
        {
            var projects = await _ecuProjectService.ListAsync();
            ProjectsGrid.ItemsSource = projects
                .Select(project =>
                {
                    var state = _ecuProjectWorkflowStatusService.Evaluate(project);
                    return new ProjectListItem(
                        project.Id,
                        project.Name,
                        $"{project.Make} {project.Model} {project.Engine}".Trim(),
                        state.StatusText,
                        state.NextStep,
                        project.Versions.Count,
                        project.MapDefinitions.Count);
                })
                .ToArray();

            var vehicles = await _vehicleService.ListAsync();
            var vehicleItems = vehicles
                .Select(vehicle => new VehicleSelectionItem(
                    vehicle.Id,
                    $"{vehicle.Make} {vehicle.Model} {vehicle.EngineCode}".Trim()))
                .ToArray();
            ProjectVehicleSelector.ItemsSource = vehicleItems;
            ProjectVehicleSelector.SelectedItem ??= vehicleItems.FirstOrDefault();
            await LoadProjectOriginalFilesAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load ECU projects.", exception);
            ProjectMessage.Text = _localizer.Text("Projects.LoadFailure");
        }
    }

    private async Task LoadProjectOriginalFilesAsync()
    {
        if (ProjectVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            ProjectOriginalFileSelector.ItemsSource = Array.Empty<EcuFileSelectionItem>();
            return;
        }

        var files = await _ecuFileService.ListByVehicleAsync(selectedVehicle.Id);
        var originalFiles = files
            .Where(file => file.FileType == EcuFileType.Original)
            .Select(ToEcuFileSelectionItem)
            .ToArray();

        ProjectOriginalFileSelector.ItemsSource = originalFiles;
        ProjectOriginalFileSelector.SelectedItem = originalFiles.FirstOrDefault();
        if (ProjectNameTextBox.Text.Length == 0)
        {
            ProjectNameTextBox.Text = $"{selectedVehicle.DisplayName} project".Trim();
        }
    }

    private async void OnProjectVehicleChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadProjectOriginalFilesAsync();
    }

    private async void OnRefreshProjectsClick(object sender, RoutedEventArgs e)
    {
        await LoadProjectsAsync();
    }

    private async void OnCreateProjectClick(object sender, RoutedEventArgs e)
    {
        ProjectMessage.Text = string.Empty;
        if (ProjectVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            ProjectMessage.Text = _localizer.Text("Projects.NoVehicle");
            return;
        }

        if (ProjectOriginalFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            ProjectMessage.Text = _localizer.Text("Projects.NoOriginalFile");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
        {
            ProjectMessage.Text = _localizer.Text("Projects.NoName");
            return;
        }

        var vehicle = await _vehicleService.GetByIdAsync(selectedVehicle.Id);
        if (vehicle is null)
        {
            ProjectMessage.Text = _localizer.Text("Projects.NoVehicle");
            return;
        }

        var originalFile = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (originalFile is null)
        {
            ProjectMessage.Text = _localizer.Text("Projects.NoOriginalFile");
            return;
        }

        var result = await _ecuProjectService.CreateAsync(new EcuProjectCreateRequest(
            vehicle.Id,
            originalFile.Id,
            ProjectNameTextBox.Text.Trim(),
            vehicle.Make,
            vehicle.Model,
            vehicle.Engine,
            vehicle.Year,
            "Unknown",
            null,
            originalFile.ReadMethod ?? "ManualWorkflowOnly",
            EmptyToNull(ProjectNotesTextBox.Text),
            "ui-project"));

        ProjectMessage.Text = result.IsSuccess
            ? _localizer.Text("Projects.CreateSuccess")
            : $"{_localizer.Text("Projects.CreateFailure")} {result.ErrorMessage}";

        if (result.IsSuccess)
        {
            await LoadProjectsAsync();
        }
    }

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectsGrid.SelectedItem is not ProjectListItem selectedProject)
        {
            return;
        }

        ProjectMessage.Text = string.Format(
            _localizer.Text("Projects.Selected"),
            selectedProject.Name,
            selectedProject.Status,
            selectedProject.VersionCount,
            selectedProject.MapDefinitionCount,
            selectedProject.NextStep);
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
        var selectedOperation = (MapWorkspaceEditOperationSelector.SelectedItem as MapEditOperationOption)?.Operation;
        var operationOptions = BuildMapEditOperationOptions();
        MapWorkspaceEditOperationSelector.ItemsSource = operationOptions;
        MapWorkspaceEditOperationSelector.SelectedItem = operationOptions.FirstOrDefault(option => option.Operation == selectedOperation) ?? operationOptions[0];
        ProjectsListTitle.Text = _localizer.Text("Projects.ListTitle");
        RefreshProjectsButton.Content = _localizer.Text("Projects.Refresh");
        ProjectFormTitle.Text = _localizer.Text("Projects.FormTitle");
        ProjectVehicleLabel.Text = _localizer.Text("Projects.Vehicle");
        ProjectOriginalFileLabel.Text = _localizer.Text("Projects.OriginalFile");
        ProjectNameLabel.Text = _localizer.Text("Projects.Name");
        ProjectNotesLabel.Text = _localizer.Text("Projects.Notes");
        CreateProjectButton.Content = _localizer.Text("Projects.Create");
        ProjectNameColumn.Header = _localizer.Text("Projects.Name");
        ProjectVehicleColumn.Header = _localizer.Text("Projects.Vehicle");
        ProjectStatusColumn.Header = _localizer.Text("Projects.Status");
        ProjectVersionsColumn.Header = _localizer.Text("Projects.Versions");
        RefreshVehicleProfileOptions();
        NewVehicleButton.Content = _localizer.Text("Vehicles.New");
        SaveVehicleButton.Content = _localizer.Text("Vehicles.Save");
        DeleteVehicleButton.Content = _localizer.Text("Common.Delete");
        RefreshVehiclesButton.Content = _localizer.Text("Vehicles.Refresh");
        VehiclesListTitle.Text = _localizer.Text("Vehicles.ListTitle");
        VehiclesEmptyText.Text = _localizer.Text("Vehicles.Empty");
        VehicleProfileLabel.Text = _localizer.Text("Vehicles.Profile");
        VehicleMakeLabel.Text = _localizer.Text("Vehicles.Make");
        VehicleModelLabel.Text = _localizer.Text("Vehicles.Model");
        VehicleYearLabel.Text = _localizer.Text("Vehicles.Year");
        VehicleFuelLabel.Text = _localizer.Text("Vehicles.Fuel");
        VehicleEngineLabel.Text = _localizer.Text("Vehicles.Engine");
        VehicleEngineCodeLabel.Text = _localizer.Text("Vehicles.EngineCode");
        VehicleVinLabel.Text = _localizer.Text("Vehicles.Vin");
        VehicleLicensePlateLabel.Text = _localizer.Text("Vehicles.LicensePlate");
        VehicleNotesLabel.Text = _localizer.Text("Vehicles.Notes");
        VehicleMakeColumn.Header = _localizer.Text("Vehicles.Make");
        VehicleModelColumn.Header = _localizer.Text("Vehicles.Model");
        VehicleEngineCodeColumn.Header = _localizer.Text("Vehicles.EngineCode");
        VehicleFormTitle.Text = _selectedVehicleId is null
            ? _localizer.Text("Vehicles.FormTitle.New")
            : _localizer.Text("Vehicles.FormTitle.Edit");
        EcuVehicleLabel.Text = _localizer.Text("Ecus.Vehicle");
        EcusListTitle.Text = _localizer.Text("Ecus.ListTitle");
        NewEcuButton.Content = _localizer.Text("Ecus.New");
        SaveEcuButton.Content = _localizer.Text("Ecus.Save");
        DeleteEcuButton.Content = _localizer.Text("Common.Delete");
        RefreshEcusButton.Content = _localizer.Text("Ecus.Refresh");
        EcuManufacturerLabel.Text = _localizer.Text("Ecus.Manufacturer");
        EcuFamilyLabel.Text = _localizer.Text("Ecus.Family");
        EcuHardwareLabel.Text = _localizer.Text("Ecus.Hardware");
        EcuSoftwareLabel.Text = _localizer.Text("Ecus.Software");
        EcuSoftwareVersionLabel.Text = _localizer.Text("Ecus.SoftwareVersion");
        EcuProtocolLabel.Text = _localizer.Text("Ecus.Protocol");
        EcuSupportStatusLabel.Text = _localizer.Text("Ecus.SupportStatus");
        EcuNotesLabel.Text = _localizer.Text("Ecus.Notes");
        EcuManufacturerColumn.Header = _localizer.Text("Ecus.Manufacturer");
        EcuFamilyColumn.Header = _localizer.Text("Ecus.Family");
        EcuSupportStatusColumn.Header = _localizer.Text("Ecus.SupportStatus");
        RefreshEcuConfidence();
        EcuFileVehicleLabel.Text = _localizer.Text("EcuFiles.Vehicle");
        EcuFileTypeLabel.Text = _localizer.Text("EcuFiles.FileType");
        EcuFileSourceLabel.Text = _localizer.Text("EcuFiles.SourceFile");
        EcuFileProgrammerLabel.Text = _localizer.Text("EcuFiles.Programmer");
        EcuFileReadMethodLabel.Text = _localizer.Text("EcuFiles.ReadMethod");
        EcuFileOriginLabel.Text = _localizer.Text("EcuFiles.Origin");
        EcuFileNotesLabel.Text = _localizer.Text("EcuFiles.Notes");
        SelectEcuFileButton.Content = _localizer.Text("EcuFiles.SelectFile");
        ImportEcuFileButton.Content = _localizer.Text("EcuFiles.Import");
        DeleteEcuFileButton.Content = _localizer.Text("Common.Delete");
        RefreshEcuFilesButton.Content = _localizer.Text("EcuFiles.Refresh");
        EcuFilesListTitle.Text = _localizer.Text("EcuFiles.ImportedFiles");
        EcuFileNameColumn.Header = _localizer.Text("EcuFiles.FileName");
        EcuFileTypeColumn.Header = _localizer.Text("EcuFiles.FileType");
        EcuFileSizeColumn.Header = _localizer.Text("EcuFiles.Size");
        EcuFileHashColumn.Header = _localizer.Text("EcuFiles.Hash");
        EcuFileChecksumColumn.Header = _localizer.Text("EcuFiles.Checksum");
        EcuFileImportedAtColumn.Header = _localizer.Text("EcuFiles.ImportedAt");
        ProgrammersListTitle.Text = _localizer.Text("Programmers.ListTitle");
        ProgrammerNameColumn.Header = _localizer.Text("Programmers.Name");
        ProgrammerConnectionColumn.Header = _localizer.Text("Programmers.ConnectionType");
        ProgrammerModeColumn.Header = _localizer.Text("Programmers.Mode");
        ProgrammerDirectReadColumn.Header = _localizer.Text("Programmers.DirectRead");
        ProgrammerDirectWriteColumn.Header = _localizer.Text("Programmers.DirectWrite");
        ProgrammerExternalSoftwareColumn.Header = _localizer.Text("Programmers.ExternalSoftware");
        ProgrammerNotesColumn.Header = _localizer.Text("Programmers.Notes");
        ComparisonVehicleLabel.Text = _localizer.Text("Comparison.Vehicle");
        ComparisonOriginalFileLabel.Text = _localizer.Text("Comparison.OriginalFile");
        ComparisonModifiedFileLabel.Text = _localizer.Text("Comparison.ModifiedFile");
        RefreshComparisonsButton.Content = _localizer.Text("Comparison.Refresh");
        RunComparisonButton.Content = _localizer.Text("Comparison.Compare");
        ComparisonResultsTitle.Text = _localizer.Text("Comparison.Results");
        ComparisonOriginalColumn.Header = _localizer.Text("Comparison.OriginalFile");
        ComparisonModifiedColumn.Header = _localizer.Text("Comparison.ModifiedFile");
        ComparisonDifferencesColumn.Header = _localizer.Text("Comparison.Differences");
        ComparisonPercentColumn.Header = _localizer.Text("Comparison.PercentChanged");
        ComparisonOffsetsColumn.Header = _localizer.Text("Comparison.Offsets");
        ComparisonBlocksColumn.Header = _localizer.Text("Comparison.Blocks");
        ComparisonResultColumn.Header = _localizer.Text("Comparison.Result");
        ComparisonComparedAtColumn.Header = _localizer.Text("Comparison.ComparedAt");
        MapWorkspaceVehicleLabel.Text = _localizer.Text("MapWorkspace.Vehicle");
        MapWorkspaceFileLabel.Text = _localizer.Text("MapWorkspace.File");
        MapWorkspaceModifiedFileLabel.Text = _localizer.Text("MapWorkspace.ModifiedFile");
        MapWorkspaceEcuLabel.Text = _localizer.Text("MapWorkspace.Ecu");
        MapWorkspaceProfileLabel.Text = _localizer.Text("MapWorkspace.Profile");
        MapWorkspacePercentageLabel.Text = _localizer.Text("MapWorkspace.Percentage");
        MapWorkspaceOffsetLabel.Text = _localizer.Text("MapWorkspace.Offset");
        MapWorkspaceLengthLabel.Text = _localizer.Text("MapWorkspace.Length");
        MapWorkspaceMapIdLabel.Text = _localizer.Text("MapWorkspace.MapId");
        MapWorkspaceMapNameLabel.Text = _localizer.Text("MapWorkspace.MapName");
        MapWorkspaceDataTypeLabel.Text = _localizer.Text("MapWorkspace.DataType");
        MapWorkspaceRowsLabel.Text = _localizer.Text("MapWorkspace.Rows");
        MapWorkspaceColumnsLabel.Text = _localizer.Text("MapWorkspace.Columns");
        MapWorkspaceDefinedMapLabel.Text = _localizer.Text("MapWorkspace.DefinedMap");
        MapWorkspaceEditOperationLabel.Text = _localizer.Text("MapWorkspace.EditOperation");
        MapWorkspaceEditValueLabel.Text = _localizer.Text("MapWorkspace.EditValue");
        MapWorkspaceMinAllowedLabel.Text = _localizer.Text("MapWorkspace.MinAllowed");
        MapWorkspaceMaxAllowedLabel.Text = _localizer.Text("MapWorkspace.MaxAllowed");
        RefreshMapWorkspaceButton.Content = _localizer.Text("MapWorkspace.Refresh");
        RefreshMapDefinitionsButton.Content = _localizer.Text("MapWorkspace.RefreshMaps");
        PreviewMapWorkspaceButton.Content = _localizer.Text("MapWorkspace.Preview");
        HexMapWorkspaceButton.Content = _localizer.Text("MapWorkspace.HexView");
        CompareMapWorkspaceButton.Content = _localizer.Text("MapWorkspace.Compare");
        AddMapDefinitionButton.Content = _localizer.Text("MapWorkspace.AddMap");
        PreviewDefinedMapButton.Content = _localizer.Text("MapWorkspace.PreviewMap");
        PreviewMapEditButton.Content = _localizer.Text("MapWorkspace.PreviewEdit");
        EvaluatePercentageIntentButton.Content = _localizer.Text("MapWorkspace.EvaluatePercentage");
        MapWorkspaceChartTitle.Text = _localizer.Text("MapWorkspace.Chart");
        MapWorkspaceTableTitle.Text = _localizer.Text("MapWorkspace.Table");
        MapWorkspaceOffsetColumn.Header = _localizer.Text("MapWorkspace.Offset");
        MapWorkspaceOriginalHexColumn.Header = _localizer.Text("MapWorkspace.OriginalHex");
        MapWorkspaceModifiedHexColumn.Header = _localizer.Text("MapWorkspace.ModifiedHex");
        MapWorkspaceDeltaColumn.Header = _localizer.Text("MapWorkspace.Delta");
        MapWorkspaceRawColumn.Header = _localizer.Text("MapWorkspace.Raw");
        MapWorkspaceScaledColumn.Header = _localizer.Text("MapWorkspace.Scaled");
        MapWorkspaceEditGateText.Text = _localizer.Text("MapWorkspace.EditBlocked");
        ReportComparisonLabel.Text = _localizer.Text("Reports.Comparison");
        RefreshReportsButton.Content = _localizer.Text("Reports.Refresh");
        GenerateReportButton.Content = _localizer.Text("Reports.Generate");
        ReportOutputLabel.Text = _localizer.Text("Reports.Output");
        NavigationItems.ItemsSource = NavigationCatalog.Sections
            .Select(section => new
            {
                section.Area,
                Title = _localizer.Text(section.TitleKey)
            })
            .ToArray();

        if (_currentArea == ApplicationArea.Programmers)
        {
            LoadProgrammerCapabilities();
        }
        else if (_currentArea == ApplicationArea.Ecus)
        {
            _ = LoadEcuVehiclesAsync();
        }
        else if (_currentArea == ApplicationArea.Projects)
        {
            _ = LoadProjectsAsync();
        }
        else if (_currentArea == ApplicationArea.Comparison)
        {
            _ = LoadComparisonResultsAsync();
        }
        else if (_currentArea == ApplicationArea.CalibrationPreview)
        {
            _ = LoadMapWorkspaceVehiclesAsync();
        }
        else if (_currentArea == ApplicationArea.Reports)
        {
            _ = LoadReportComparisonsAsync();
        }
    }

    private async Task LoadEcuVehiclesAsync()
    {
        try
        {
            var vehicles = await _vehicleService.ListAsync();
            var selectedVehicleId = (EcuVehicleSelector.SelectedItem as VehicleSelectionItem)?.Id;
            var items = vehicles
                .Select(vehicle => new VehicleSelectionItem(
                    vehicle.Id,
                    $"{vehicle.Make} {vehicle.Model} {vehicle.EngineCode}".Trim()))
                .ToArray();

            EcuVehicleSelector.ItemsSource = items;
            EcuVehicleSelector.SelectedItem = items.FirstOrDefault(item => item.Id == selectedVehicleId) ?? items.FirstOrDefault();
            await LoadEcusForSelectedVehicleAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load ECU vehicles.", exception);
            EcuMessage.Text = _localizer.Text("Ecus.LoadFailure");
        }
    }

    private async Task LoadEcusForSelectedVehicleAsync()
    {
        if (EcuVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            EcusGrid.ItemsSource = Array.Empty<EcuListItem>();
            StartNewEcu();
            return;
        }

        var ecus = await _ecuInfoService.ListByVehicleAsync(selectedVehicle.Id);
        EcusGrid.ItemsSource = ecus
            .Select(ecu => new EcuListItem(
                ecu.Id,
                ecu.Manufacturer ?? "Unknown",
                ecu.EcuFamily ?? "Unknown",
                ecu.SupportStatus.ToString()))
            .ToArray();

        if (_selectedEcuId is null)
        {
            StartNewEcu();
        }
    }

    private async void OnEcuVehicleChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadEcusForSelectedVehicleAsync();
    }

    private async void OnEcuSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingEcu || EcusGrid.SelectedItem is not EcuListItem selectedEcu)
        {
            return;
        }

        var ecu = await _ecuInfoService.GetByIdAsync(selectedEcu.Id);
        if (ecu is not null)
        {
            LoadEcuIntoForm(ecu);
        }
    }

    private void OnNewEcuClick(object sender, RoutedEventArgs e) => StartNewEcu();

    private async void OnRefreshEcusClick(object sender, RoutedEventArgs e) => await LoadEcuVehiclesAsync();

    private async void OnSaveEcuClick(object sender, RoutedEventArgs e)
    {
        EcuMessage.Text = string.Empty;

        if (EcuVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            EcuMessage.Text = _localizer.Text("Ecus.NoVehicle");
            return;
        }

        var ecu = BuildEcuFromForm(selectedVehicle.Id);
        var result = _selectedEcuId is null
            ? await _ecuInfoService.CreateAsync(ecu)
            : await _ecuInfoService.UpdateAsync(ecu);

        EcuMessage.Text = result.IsSuccess
            ? _localizer.Text("Ecus.SaveSuccess")
            : $"{_localizer.Text("Ecus.SaveFailure")} {result.ErrorMessage}";

        if (result.IsSuccess && result.Value is not null)
        {
            _selectedEcuId = result.Value.Id;
            await LoadEcusForSelectedVehicleAsync();
        }
    }

    private async void OnDeleteEcuClick(object sender, RoutedEventArgs e)
    {
        EcuMessage.Text = string.Empty;
        if (_selectedEcuId is null)
        {
            EcuMessage.Text = _localizer.Text("Ecus.NoSelection");
            return;
        }

        if (!ConfirmDelete())
        {
            return;
        }

        var result = await _ecuInfoService.DeleteAsync(_selectedEcuId.Value);
        EcuMessage.Text = result.IsSuccess
            ? _localizer.Text("Ecus.DeleteSuccess")
            : $"{_localizer.Text("Ecus.DeleteFailure")} {result.ErrorMessage}";
        if (result.IsSuccess)
        {
            _selectedEcuId = null;
            await LoadEcuVehiclesAsync();
        }
    }

    private void OnEcuFieldChanged(object sender, RoutedEventArgs e)
    {
        if (!_isLoadingEcu)
        {
            RefreshEcuConfidence();
        }
    }

    private void StartNewEcu()
    {
        _isLoadingEcu = true;
        _selectedEcuId = null;
        EcusGrid.SelectedItem = null;
        EcuManufacturerTextBox.Text = string.Empty;
        EcuFamilyTextBox.Text = string.Empty;
        EcuHardwareTextBox.Text = string.Empty;
        EcuSoftwareTextBox.Text = string.Empty;
        EcuSoftwareVersionTextBox.Text = string.Empty;
        EcuProtocolTextBox.Text = string.Empty;
        EcuSupportStatusSelector.SelectedItem = SupportStatus.Unknown;
        EcuNotesTextBox.Text = string.Empty;
        EcuMessage.Text = string.Empty;
        _isLoadingEcu = false;
        RefreshEcuConfidence();
    }

    private void LoadEcuIntoForm(EcuInfo ecu)
    {
        _isLoadingEcu = true;
        _selectedEcuId = ecu.Id;
        EcuManufacturerTextBox.Text = ecu.Manufacturer;
        EcuFamilyTextBox.Text = ecu.EcuFamily;
        EcuHardwareTextBox.Text = ecu.HardwareReference;
        EcuSoftwareTextBox.Text = ecu.SoftwareReference;
        EcuSoftwareVersionTextBox.Text = ecu.SoftwareVersion;
        EcuProtocolTextBox.Text = ecu.Protocol;
        EcuSupportStatusSelector.SelectedItem = ecu.SupportStatus;
        EcuNotesTextBox.Text = ecu.Notes;
        EcuMessage.Text = string.Empty;
        _isLoadingEcu = false;
        RefreshEcuConfidence();
    }

    private EcuInfo BuildEcuFromForm(Guid vehicleId) =>
        new()
        {
            Id = _selectedEcuId ?? Guid.NewGuid(),
            VehicleId = vehicleId,
            Manufacturer = EmptyToNull(EcuManufacturerTextBox.Text),
            EcuFamily = EmptyToNull(EcuFamilyTextBox.Text),
            HardwareReference = EmptyToNull(EcuHardwareTextBox.Text),
            SoftwareReference = EmptyToNull(EcuSoftwareTextBox.Text),
            SoftwareVersion = EmptyToNull(EcuSoftwareVersionTextBox.Text),
            Protocol = EmptyToNull(EcuProtocolTextBox.Text),
            SupportStatus = EcuSupportStatusSelector.SelectedItem is SupportStatus status ? status : SupportStatus.Unknown,
            Notes = EmptyToNull(EcuNotesTextBox.Text)
        };

    private void RefreshEcuConfidence()
    {
        if (EcuConfidenceText is null)
        {
            return;
        }

        var ecu = BuildEcuFromForm(Guid.NewGuid());
        var result = _ecuInfoService.EvaluateIdentification(ecu);
        EcuConfidenceText.Text = $"{_localizer.Text("Ecus.Confidence")}: {result.Confidence}";
        EcuMissingEvidenceText.Text = $"{_localizer.Text("Ecus.MissingEvidence")}: {string.Join(", ", result.MissingEvidence)}";
    }

    private async Task LoadReportComparisonsAsync()
    {
        try
        {
            var comparisons = await _binaryComparisonService.ListAsync();
            var selectedId = (ReportComparisonSelector.SelectedItem as ReportComparisonSelectionItem)?.Id;
            var items = comparisons
                .Select(comparison => new ReportComparisonSelectionItem(
                    comparison.Id,
                    $"{comparison.ComparedAt.LocalDateTime:yyyy-MM-dd HH:mm} - {comparison.Result} - {comparison.PercentChanged:0.######}%"))
                .ToArray();

            ReportComparisonSelector.ItemsSource = items;
            ReportComparisonSelector.SelectedItem = items.FirstOrDefault(item => item.Id == selectedId) ?? items.FirstOrDefault();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load report comparisons.", exception);
            ReportMessage.Text = _localizer.Text("Reports.LoadFailure");
        }
    }

    private async void OnRefreshReportsClick(object sender, RoutedEventArgs e)
    {
        await LoadReportComparisonsAsync();
    }

    private async void OnGenerateReportClick(object sender, RoutedEventArgs e)
    {
        ReportMessage.Text = string.Empty;
        ReportOutputTextBox.Text = string.Empty;

        if (ReportComparisonSelector.SelectedItem is not ReportComparisonSelectionItem selectedComparison)
        {
            ReportMessage.Text = _localizer.Text("Reports.NoComparison");
            return;
        }

        var result = await _reportService.GenerateTechnicalReportAsync(
            new TechnicalReportRequest(selectedComparison.Id, _configuration.ReportsDirectory));
        ReportMessage.Text = result.IsSuccess
            ? _localizer.Text("Reports.Success")
            : $"{_localizer.Text("Reports.Failure")} {result.ErrorMessage}";
        ReportOutputTextBox.Text = result.Value ?? string.Empty;
    }

    private async Task LoadComparisonVehiclesAsync()
    {
        try
        {
            var vehicles = await _vehicleService.ListAsync();
            var selectedVehicleId = (ComparisonVehicleSelector.SelectedItem as VehicleSelectionItem)?.Id;
            var items = vehicles
                .Select(vehicle => new VehicleSelectionItem(
                    vehicle.Id,
                    $"{vehicle.Make} {vehicle.Model} {vehicle.EngineCode}".Trim()))
                .ToArray();

            ComparisonVehicleSelector.ItemsSource = items;
            ComparisonVehicleSelector.SelectedItem = items.FirstOrDefault(item => item.Id == selectedVehicleId) ?? items.FirstOrDefault();

            await LoadComparisonFilesForSelectedVehicleAsync();
            await LoadComparisonResultsAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load comparison vehicles.", exception);
            ComparisonMessage.Text = _localizer.Text("Comparison.LoadFailure");
        }
    }

    private async Task LoadComparisonFilesForSelectedVehicleAsync()
    {
        if (ComparisonVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            ComparisonOriginalFileSelector.ItemsSource = Array.Empty<EcuFileSelectionItem>();
            ComparisonModifiedFileSelector.ItemsSource = Array.Empty<EcuFileSelectionItem>();
            return;
        }

        var files = await _ecuFileService.ListByVehicleAsync(selectedVehicle.Id);
        var originalFiles = files
            .Where(file => file.FileType == EcuFileType.Original)
            .Select(ToEcuFileSelectionItem)
            .ToArray();
        var modifiedFiles = files
            .Where(file => file.FileType == EcuFileType.Modified)
            .Select(ToEcuFileSelectionItem)
            .ToArray();

        ComparisonOriginalFileSelector.ItemsSource = originalFiles;
        ComparisonModifiedFileSelector.ItemsSource = modifiedFiles;
        ComparisonOriginalFileSelector.SelectedItem = originalFiles.FirstOrDefault();
        ComparisonModifiedFileSelector.SelectedItem = modifiedFiles.FirstOrDefault();
    }

    private async Task LoadComparisonResultsAsync()
    {
        var comparisons = await _binaryComparisonService.ListAsync();
        ComparisonsGrid.ItemsSource = comparisons
            .Select(comparison => new ComparisonListItem(
                comparison.OriginalFileId.ToString("N")[..12],
                comparison.ModifiedFileId.ToString("N")[..12],
                comparison.DifferenceCount,
                $"{comparison.PercentChanged:0.######}%",
                comparison.DifferenceSummary,
                comparison.DifferenceBlockSummary,
                comparison.Result,
                comparison.ComparedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm")))
            .ToArray();
    }

    private async void OnComparisonVehicleChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadComparisonFilesForSelectedVehicleAsync();
    }

    private async void OnRefreshComparisonsClick(object sender, RoutedEventArgs e)
    {
        await LoadComparisonVehiclesAsync();
    }

    private async void OnRunComparisonClick(object sender, RoutedEventArgs e)
    {
        ComparisonMessage.Text = string.Empty;

        if (ComparisonVehicleSelector.SelectedItem is not VehicleSelectionItem)
        {
            ComparisonMessage.Text = _localizer.Text("Comparison.NoVehicle");
            return;
        }

        if (ComparisonOriginalFileSelector.SelectedItem is not EcuFileSelectionItem originalFile)
        {
            ComparisonMessage.Text = _localizer.Text("Comparison.NoOriginal");
            return;
        }

        if (ComparisonModifiedFileSelector.SelectedItem is not EcuFileSelectionItem modifiedFile)
        {
            ComparisonMessage.Text = _localizer.Text("Comparison.NoModified");
            return;
        }

        var result = await _binaryComparisonService.CompareAsync(
            new BinaryComparisonRequest(originalFile.Id, modifiedFile.Id));
        ComparisonMessage.Text = result.IsSuccess
            ? _localizer.Text("Comparison.Success")
            : $"{_localizer.Text("Comparison.Failure")} {result.ErrorMessage}";

        if (result.IsSuccess)
        {
            await LoadComparisonResultsAsync();
        }
    }

    private async Task LoadMapWorkspaceVehiclesAsync()
    {
        try
        {
            var vehicles = await _vehicleService.ListAsync();
            var selectedVehicleId = (MapWorkspaceVehicleSelector.SelectedItem as VehicleSelectionItem)?.Id;
            var items = vehicles
                .Select(vehicle => new VehicleSelectionItem(
                    vehicle.Id,
                    $"{vehicle.Make} {vehicle.Model} {vehicle.EngineCode}".Trim()))
                .ToArray();

            MapWorkspaceVehicleSelector.ItemsSource = items;
            MapWorkspaceVehicleSelector.SelectedItem = items.FirstOrDefault(item => item.Id == selectedVehicleId) ?? items.FirstOrDefault();
            await LoadMapWorkspaceFilesForSelectedVehicleAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load map workspace vehicles.", exception);
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.LoadFailure");
        }
    }

    private async Task LoadMapWorkspaceFilesForSelectedVehicleAsync()
    {
        if (MapWorkspaceVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            MapWorkspaceFileSelector.ItemsSource = Array.Empty<EcuFileSelectionItem>();
            MapWorkspaceModifiedFileSelector.ItemsSource = Array.Empty<EcuFileSelectionItem>();
            MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
            DrawMapWorkspaceChart([]);
            return;
        }

        var files = await _ecuFileService.ListByVehicleAsync(selectedVehicle.Id);
        var ecus = await _ecuInfoService.ListByVehicleAsync(selectedVehicle.Id);
        var originalFiles = files
            .Where(file => file.FileType == EcuFileType.Original)
            .Select(ToEcuFileSelectionItem)
            .ToArray();
        var modifiedFiles = files
            .Where(file => file.FileType == EcuFileType.Modified)
            .Select(ToEcuFileSelectionItem)
            .ToArray();
        var ecuItems = ecus
            .Select(ecu => new EcuSelectionItem(
                ecu.Id,
                $"{ecu.Manufacturer ?? "Unknown"} {ecu.EcuFamily ?? "Unknown"} {ecu.SoftwareVersion ?? string.Empty}".Trim()))
            .ToArray();

        MapWorkspaceFileSelector.ItemsSource = originalFiles;
        MapWorkspaceFileSelector.SelectedItem = originalFiles.FirstOrDefault();
        MapWorkspaceModifiedFileSelector.ItemsSource = modifiedFiles;
        MapWorkspaceModifiedFileSelector.SelectedItem = modifiedFiles.FirstOrDefault();
        MapWorkspaceEcuSelector.ItemsSource = ecuItems;
        MapWorkspaceEcuSelector.SelectedItem = ecuItems.FirstOrDefault();
        await LoadMapWorkspaceDefinitionsForSelectedFileAsync();
        MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.ReadOnlyNotice");
        MapWorkspaceEditGateText.Text = _localizer.Text("MapWorkspace.EditBlocked");
    }

    private async void OnMapWorkspaceVehicleChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadMapWorkspaceFilesForSelectedVehicleAsync();
    }

    private async void OnRefreshMapWorkspaceClick(object sender, RoutedEventArgs e)
    {
        await LoadMapWorkspaceVehiclesAsync();
    }

    private async void OnRefreshMapDefinitionsClick(object sender, RoutedEventArgs e)
    {
        await LoadMapWorkspaceDefinitionsForSelectedFileAsync();
    }

    private async void OnPreviewMapWorkspaceClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;
        MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
        DrawMapWorkspaceChart([]);

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        if (!TryParseOffset(MapWorkspaceOffsetTextBox.Text, out var startOffset))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidOffset");
            return;
        }

        if (!int.TryParse(MapWorkspaceLengthTextBox.Text.Trim(), out var length))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidLength");
            return;
        }

        var file = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (file is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        var result = await _mapWorkspacePreviewService.PreviewRawBytesAsync(
            new MapWorkspacePreviewRequest(file.FilePath, startOffset, length));

        if (!result.IsSuccess)
        {
            MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.BlockReasons);
            return;
        }

        var rows = result.Points
            .Select(point => new MapWorkspacePointListItem(
                $"0x{point.Offset:X8}",
                $"0x{point.RawByte:X2}",
                string.Empty,
                string.Empty,
                point.RawByte,
                $"{point.ScaledValue:0.######} {result.Unit}"))
            .ToArray();

        MapWorkspaceGrid.ItemsSource = rows;
        DrawMapWorkspaceChart(result.Points);
        MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.Messages);
        MapWorkspaceEditGateText.Text = string.Join(Environment.NewLine, result.BlockReasons);
    }

    private async void OnHexMapWorkspaceClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;
        MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
        DrawMapWorkspaceChart([]);

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        if (!TryParseOffset(MapWorkspaceOffsetTextBox.Text, out var startOffset))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidOffset");
            return;
        }

        if (!int.TryParse(MapWorkspaceLengthTextBox.Text.Trim(), out var length))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidLength");
            return;
        }

        var file = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (file is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        var result = await _mapWorkspacePreviewService.PreviewHexAsync(
            new MapWorkspaceHexViewRequest(file.FilePath, startOffset, length));

        if (!result.IsSuccess)
        {
            MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.BlockReasons);
            return;
        }

        MapWorkspaceGrid.ItemsSource = result.Rows
            .Select(row => new MapWorkspacePointListItem(
                $"0x{row.StartOffset:X8}",
                string.Join(" ", row.HexBytes),
                string.Empty,
                string.Empty,
                row.HexBytes.Count,
                row.AsciiPreview))
            .ToArray();
        MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.Messages);
        MapWorkspaceEditGateText.Text = string.Join(Environment.NewLine, result.BlockReasons);
    }

    private async void OnCompareMapWorkspaceClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;
        MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
        DrawMapWorkspaceChart([]);

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem originalSelection)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        if (MapWorkspaceModifiedFileSelector.SelectedItem is not EcuFileSelectionItem modifiedSelection)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoModifiedFile");
            return;
        }

        if (!TryParseOffset(MapWorkspaceOffsetTextBox.Text, out var startOffset))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidOffset");
            return;
        }

        if (!int.TryParse(MapWorkspaceLengthTextBox.Text.Trim(), out var length))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidLength");
            return;
        }

        var originalFile = await _ecuFileService.GetByIdAsync(originalSelection.Id);
        var modifiedFile = await _ecuFileService.GetByIdAsync(modifiedSelection.Id);
        if (originalFile is null || modifiedFile is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        var result = await _mapWorkspacePreviewService.CompareRawBytesAsync(
            new MapWorkspaceComparisonRequest(
                originalFile.FilePath,
                modifiedFile.FilePath,
                startOffset,
                length));

        if (!result.IsSuccess)
        {
            MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.BlockReasons);
            return;
        }

        MapWorkspaceGrid.ItemsSource = result.Points
            .Select(point => new MapWorkspacePointListItem(
                $"0x{point.Offset:X8}",
                FormatNullableByte(point.OriginalByte),
                FormatNullableByte(point.ModifiedByte),
                point.Difference?.ToString() ?? "Missing",
                point.OriginalByte ?? 0,
                string.Empty))
            .ToArray();
        DrawMapWorkspaceComparisonChart(result.Points);
        MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.Messages);
        MapWorkspaceEditGateText.Text = string.Join(Environment.NewLine, result.BlockReasons);
    }

    private async void OnAddMapDefinitionClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;

        var request = await TryBuildMapDefinitionRequestAsync();
        if (request is null)
        {
            return;
        }

        var result = await _ecuProjectMapDefinitionService.CreateAsync(request);
        MapWorkspaceStatusText.Text = result.IsSuccess
            ? _localizer.Text("MapWorkspace.MapSaved")
            : $"{_localizer.Text("MapWorkspace.MapSaveFailure")} {result.ErrorMessage}";
        MapWorkspaceEditGateText.Text = _localizer.Text("MapWorkspace.EditBlocked");
        await LoadMapWorkspaceDefinitionsForSelectedFileAsync();
    }

    private async void OnPreviewDefinedMapClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;
        MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
        DrawMapWorkspaceChart([]);

        var project = await EnsureMapWorkspaceProjectAsync();
        if (project is null)
        {
            return;
        }

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        var file = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (file is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return;
        }

        var mapId = MapWorkspaceMapIdTextBox.Text.Trim();
        var result = await _ecuProjectMapSnapshotService.CreateSnapshotAsync(
            new EcuProjectMapSnapshotRequest(project.Id, mapId, file.FilePath));
        if (!result.IsSuccess)
        {
            MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.BlockReasons);
            return;
        }

        MapWorkspaceGrid.ItemsSource = result.Cells
            .Select(cell => new MapWorkspacePointListItem(
                $"0x{cell.Offset:X8}",
                cell.RawValue.ToString(),
                string.Empty,
                string.Empty,
                ToChartByte(cell.ConvertedValue),
                $"{cell.ConvertedValue:0.######} {result.Unit}"))
            .ToArray();
        DrawMapWorkspaceSnapshotChart(result.Cells);
        MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, result.Messages);
        MapWorkspaceEditGateText.Text = string.Join(Environment.NewLine, result.BlockReasons);
    }

    private async void OnPreviewMapEditClick(object sender, RoutedEventArgs e)
    {
        MapWorkspaceStatusText.Text = string.Empty;
        MapWorkspaceGrid.ItemsSource = Array.Empty<MapWorkspacePointListItem>();
        DrawMapWorkspaceChart([]);

        var snapshot = await CreateSelectedMapSnapshotAsync();
        if (snapshot is null)
        {
            return;
        }

        if (MapWorkspaceEditOperationSelector.SelectedItem is not MapEditOperationOption operation)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoEditOperation");
            return;
        }

        if (!decimal.TryParse(MapWorkspaceEditValueTextBox.Text.Trim(), out var editValue))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidEditValue");
            return;
        }

        if (!decimal.TryParse(MapWorkspaceMinAllowedTextBox.Text.Trim(), out var minAllowed))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidMinAllowed");
            return;
        }

        if (!decimal.TryParse(MapWorkspaceMaxAllowedTextBox.Text.Trim(), out var maxAllowed))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidMaxAllowed");
            return;
        }

        var result = _ecuProjectMapEditPreviewService.Preview(new EcuProjectMapEditPreviewRequest(
            snapshot,
            StartRow: 0,
            StartColumn: 0,
            RowCount: snapshot.RowCount,
            ColumnCount: snapshot.ColumnCount,
            operation.Operation,
            editValue,
            minAllowed,
            maxAllowed));

        MapWorkspaceGrid.ItemsSource = result.Cells
            .Select(cell => new MapWorkspacePointListItem(
                $"0x{cell.Offset:X8}",
                $"{cell.CurrentValue:0.######}",
                $"{cell.ProposedValue:0.######}",
                $"{cell.Delta:0.######}",
                ToChartByte(cell.ProposedValue),
                string.Empty))
            .ToArray();

        DrawMapWorkspaceEditPreviewChart(result.Cells);
        MapWorkspaceStatusText.Text = result.IsAllowed
            ? string.Join(Environment.NewLine, result.Messages)
            : string.Join(Environment.NewLine, result.BlockReasons);
        MapWorkspaceEditGateText.Text = string.Join(Environment.NewLine, result.BlockReasons);
    }

    private async Task<EcuProjectMapDefinitionCreateRequest?> TryBuildMapDefinitionRequestAsync()
    {
        var project = await EnsureMapWorkspaceProjectAsync();
        if (project is null)
        {
            return null;
        }

        if (!TryParseOffset(MapWorkspaceOffsetTextBox.Text, out var startOffset))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidOffset");
            return null;
        }

        if (!int.TryParse(MapWorkspaceLengthTextBox.Text.Trim(), out var length))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidLength");
            return null;
        }

        if (!int.TryParse(MapWorkspaceRowsTextBox.Text.Trim(), out var rows) || rows <= 0)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidRows");
            return null;
        }

        if (!int.TryParse(MapWorkspaceColumnsTextBox.Text.Trim(), out var columns) || columns <= 0)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidColumns");
            return null;
        }

        var mapId = MapWorkspaceMapIdTextBox.Text.Trim();
        var mapName = MapWorkspaceMapNameTextBox.Text.Trim();
        var dataType = MapWorkspaceDataTypeSelector.SelectedItem as string ?? "UInt8";

        return new EcuProjectMapDefinitionCreateRequest(
            project.Id,
            mapId,
            mapName,
            mapName,
            "Candidate Maps",
            startOffset,
            length,
            rows,
            columns,
            dataType,
            dataType is "UInt8" or "Int8" ? "NotApplicable" : "BigEndian",
            1m,
            0m,
            "raw",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            string.Empty,
            "manual-ui-project",
            "Unknown",
            "Manual UI candidate. Not verified for calibration export.");
    }

    private async Task<EcuProjectMapSnapshotResult?> CreateSelectedMapSnapshotAsync()
    {
        var project = await EnsureMapWorkspaceProjectAsync();
        if (project is null)
        {
            return null;
        }

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return null;
        }

        var file = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (file is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return null;
        }

        var mapId = MapWorkspaceMapIdTextBox.Text.Trim();
        var snapshot = await _ecuProjectMapSnapshotService.CreateSnapshotAsync(
            new EcuProjectMapSnapshotRequest(project.Id, mapId, file.FilePath));
        if (!snapshot.IsSuccess)
        {
            MapWorkspaceStatusText.Text = string.Join(Environment.NewLine, snapshot.BlockReasons);
            return null;
        }

        return snapshot;
    }

    private async Task LoadMapWorkspaceDefinitionsForSelectedFileAsync()
    {
        MapWorkspaceDefinedMapSelector.ItemsSource = Array.Empty<MapDefinitionSelectionItem>();

        if (MapWorkspaceVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle
            || MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            return;
        }

        var projects = await _ecuProjectService.ListByVehicleAsync(selectedVehicle.Id);
        var project = projects.FirstOrDefault(item => item.OriginalFileId == selectedFile.Id);
        if (project is null)
        {
            return;
        }

        var definitions = await _ecuProjectMapDefinitionService.ListByProjectAsync(project.Id);
        var items = definitions
            .Select(definition => new MapDefinitionSelectionItem(
                definition.MapId,
                $"{definition.MapId} - {definition.DisplayName}",
                definition.DisplayName,
                definition.StartOffset,
                definition.Length,
                definition.RowCount,
                definition.ColumnCount,
                definition.DataType))
            .ToArray();

        MapWorkspaceDefinedMapSelector.ItemsSource = items;
        MapWorkspaceDefinedMapSelector.SelectedItem = items.FirstOrDefault();
    }

    private void OnMapWorkspaceDefinedMapChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MapWorkspaceDefinedMapSelector.SelectedItem is not MapDefinitionSelectionItem selectedMap)
        {
            return;
        }

        MapWorkspaceMapIdTextBox.Text = selectedMap.MapId;
        MapWorkspaceMapNameTextBox.Text = selectedMap.Name;
        MapWorkspaceOffsetTextBox.Text = $"0x{selectedMap.StartOffset:X}";
        MapWorkspaceLengthTextBox.Text = selectedMap.Length.ToString();
        MapWorkspaceRowsTextBox.Text = selectedMap.RowCount?.ToString() ?? "1";
        MapWorkspaceColumnsTextBox.Text = selectedMap.ColumnCount?.ToString() ?? selectedMap.Length.ToString();
        MapWorkspaceDataTypeSelector.SelectedItem = selectedMap.DataType;
    }

    private async Task<EcuProject?> EnsureMapWorkspaceProjectAsync()
    {
        if (MapWorkspaceVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoVehicle");
            return null;
        }

        if (MapWorkspaceFileSelector.SelectedItem is not EcuFileSelectionItem selectedFile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return null;
        }

        var originalFile = await _ecuFileService.GetByIdAsync(selectedFile.Id);
        if (originalFile is null)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoFile");
            return null;
        }

        var existingProjects = await _ecuProjectService.ListByVehicleAsync(selectedVehicle.Id);
        var existingProject = existingProjects.FirstOrDefault(project => project.OriginalFileId == originalFile.Id);
        if (existingProject is not null)
        {
            return existingProject;
        }

        var createResult = await _ecuProjectService.CreateAsync(new EcuProjectCreateRequest(
            selectedVehicle.Id,
            originalFile.Id,
            $"{selectedVehicle.DisplayName} - {originalFile.FileName}",
            selectedVehicle.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown",
            selectedVehicle.DisplayName,
            string.Empty,
            null,
            "Unknown",
            null,
            originalFile.ReadMethod ?? "ManualWorkflowOnly",
            "Automatically created from Map Workspace.",
            "map-workspace"));

        if (!createResult.IsSuccess || createResult.Value is null)
        {
            MapWorkspaceStatusText.Text = createResult.ErrorMessage ?? _localizer.Text("MapWorkspace.LoadFailure");
            return null;
        }

        return createResult.Value;
    }

    private async void OnEvaluatePercentageIntentClick(object sender, RoutedEventArgs e)
    {
        if (MapWorkspaceVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoVehicle");
            return;
        }

        if (MapWorkspaceEcuSelector.SelectedItem is not EcuSelectionItem selectedEcu)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoEcu");
            return;
        }

        if (MapWorkspaceProfileSelector.SelectedItem is not CalibrationProfileOption selectedProfile)
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.NoProfile");
            return;
        }

        if (!decimal.TryParse(MapWorkspacePercentageTextBox.Text.Trim(), out var percentage))
        {
            MapWorkspaceStatusText.Text = _localizer.Text("MapWorkspace.InvalidPercentage");
            return;
        }

        var result = await _percentageIntentEngine.EvaluateAsync(new PercentageIntentRequest(
            selectedVehicle.Id,
            selectedEcu.Id,
            selectedProfile.Key,
            percentage));

        MapWorkspaceStatusText.Text = result.IsAllowed
            ? _localizer.Text("MapWorkspace.PercentageAllowed")
            : _localizer.Text("MapWorkspace.PercentageBlocked");
        MapWorkspaceEditGateText.Text = string.Join(
            Environment.NewLine,
            result.SafetyReport.BlockReasons.Count == 0
                ? result.SafetyReport.Messages
                : result.SafetyReport.BlockReasons);
    }

    private void DrawMapWorkspaceChart(IReadOnlyList<MapWorkspacePreviewPoint> points)
    {
        MapWorkspaceChartCanvas.Children.Clear();
        if (points.Count == 0)
        {
            return;
        }

        var width = Math.Max(1, MapWorkspaceChartCanvas.ActualWidth);
        if (width < 10)
        {
            width = 640;
        }

        var height = Math.Max(1, MapWorkspaceChartCanvas.Height);
        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(42, 111, 151)),
            StrokeThickness = 2
        };

        for (var index = 0; index < points.Count; index++)
        {
            var x = points.Count == 1 ? width / 2 : index * width / (points.Count - 1);
            var y = height - (double)points[index].RawByte * height / 255d;
            polyline.Points.Add(new Point(x, y));
        }

        MapWorkspaceChartCanvas.Children.Add(polyline);
    }

    private void DrawMapWorkspaceComparisonChart(IReadOnlyList<MapWorkspaceComparisonPoint> points)
    {
        MapWorkspaceChartCanvas.Children.Clear();
        if (points.Count == 0)
        {
            return;
        }

        var width = Math.Max(1, MapWorkspaceChartCanvas.ActualWidth);
        if (width < 10)
        {
            width = 640;
        }

        var height = Math.Max(1, MapWorkspaceChartCanvas.Height);
        AddComparisonPolyline(points, point => point.OriginalByte, Color.FromRgb(42, 111, 151), width, height);
        AddComparisonPolyline(points, point => point.ModifiedByte, Color.FromRgb(188, 71, 73), width, height);
    }

    private void AddComparisonPolyline(
        IReadOnlyList<MapWorkspaceComparisonPoint> points,
        Func<MapWorkspaceComparisonPoint, byte?> selector,
        Color color,
        double width,
        double height)
    {
        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2
        };

        for (var index = 0; index < points.Count; index++)
        {
            var value = selector(points[index]);
            if (value is null)
            {
                continue;
            }

            var x = points.Count == 1 ? width / 2 : index * width / (points.Count - 1);
            var y = height - value.Value * height / 255d;
            polyline.Points.Add(new Point(x, y));
        }

        MapWorkspaceChartCanvas.Children.Add(polyline);
    }

    private void DrawMapWorkspaceSnapshotChart(IReadOnlyList<EcuProjectMapSnapshotCell> cells)
    {
        MapWorkspaceChartCanvas.Children.Clear();
        if (cells.Count == 0)
        {
            return;
        }

        var width = Math.Max(1, MapWorkspaceChartCanvas.ActualWidth);
        if (width < 10)
        {
            width = 640;
        }

        var height = Math.Max(1, MapWorkspaceChartCanvas.Height);
        var min = cells.Min(cell => cell.ConvertedValue);
        var max = cells.Max(cell => cell.ConvertedValue);
        var range = max - min;
        if (range <= 0)
        {
            range = 1;
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(42, 111, 151)),
            StrokeThickness = 2
        };

        for (var index = 0; index < cells.Count; index++)
        {
            var x = cells.Count == 1 ? width / 2 : index * width / (cells.Count - 1);
            var normalized = (double)((cells[index].ConvertedValue - min) / range);
            var y = height - normalized * height;
            polyline.Points.Add(new Point(x, y));
        }

        MapWorkspaceChartCanvas.Children.Add(polyline);
    }

    private void DrawMapWorkspaceEditPreviewChart(IReadOnlyList<EcuProjectMapEditPreviewCell> cells)
    {
        MapWorkspaceChartCanvas.Children.Clear();
        if (cells.Count == 0)
        {
            return;
        }

        var width = Math.Max(1, MapWorkspaceChartCanvas.ActualWidth);
        if (width < 10)
        {
            width = 640;
        }

        var height = Math.Max(1, MapWorkspaceChartCanvas.Height);
        AddEditPreviewPolyline(cells, cell => cell.CurrentValue, Color.FromRgb(42, 111, 151), width, height);
        AddEditPreviewPolyline(cells, cell => cell.ProposedValue, Color.FromRgb(188, 71, 73), width, height);
    }

    private void AddEditPreviewPolyline(
        IReadOnlyList<EcuProjectMapEditPreviewCell> cells,
        Func<EcuProjectMapEditPreviewCell, decimal> selector,
        Color color,
        double width,
        double height)
    {
        var min = cells.Min(selector);
        var max = cells.Max(selector);
        var range = max - min;
        if (range <= 0)
        {
            range = 1;
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2
        };

        for (var index = 0; index < cells.Count; index++)
        {
            var x = cells.Count == 1 ? width / 2 : index * width / (cells.Count - 1);
            var normalized = (double)((selector(cells[index]) - min) / range);
            var y = height - normalized * height;
            polyline.Points.Add(new Point(x, y));
        }

        MapWorkspaceChartCanvas.Children.Add(polyline);
    }

    private static int ToChartByte(decimal value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (int)Math.Round(value);
    }

    private static string FormatNullableByte(byte? value) =>
        value is null ? "Missing" : $"0x{value.Value:X2}";

    private static bool TryParseOffset(string value, out long offset)
    {
        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out offset);
        }

        return long.TryParse(value, out offset);
    }

    private async Task LoadEcuFileVehiclesAsync()
    {
        try
        {
            var vehicles = await _vehicleService.ListAsync();
            var selectedVehicleId = (EcuFileVehicleSelector.SelectedItem as VehicleSelectionItem)?.Id;
            var items = vehicles
                .Select(vehicle => new VehicleSelectionItem(
                    vehicle.Id,
                    $"{vehicle.Make} {vehicle.Model} {vehicle.EngineCode}".Trim()))
                .ToArray();

            EcuFileVehicleSelector.ItemsSource = items;
            EcuFileVehicleSelector.SelectedItem = items.FirstOrDefault(item => item.Id == selectedVehicleId) ?? items.FirstOrDefault();

            await LoadEcuFilesForSelectedVehicleAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load ECU file vehicles.", exception);
            EcuFileMessage.Text = _localizer.Text("EcuFiles.LoadFailure");
        }
    }

    private async Task LoadEcuFilesForSelectedVehicleAsync()
    {
        if (EcuFileVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            EcuFilesGrid.ItemsSource = Array.Empty<EcuFileListItem>();
            return;
        }

        var files = await _ecuFileService.ListByVehicleAsync(selectedVehicle.Id);
        EcuFilesGrid.ItemsSource = files
            .Select(file => new EcuFileListItem(
                file.Id,
                file.FileName,
                file.FileType.ToString(),
                file.SizeBytes,
                file.Sha256Hash,
                file.ChecksumStatus.ToString(),
                file.ImportedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm")))
            .ToArray();
    }

    private async void OnEcuFileVehicleChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadEcuFilesForSelectedVehicleAsync();
    }

    private void OnSelectEcuFileClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = _localizer.Text("EcuFiles.SelectFile"),
            Filter = "ECU files (*.bin;*.ori;*.mod)|*.bin;*.ori;*.mod|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            EcuFileSourceTextBox.Text = dialog.FileName;
        }
    }

    private async void OnRefreshEcuFilesClick(object sender, RoutedEventArgs e)
    {
        await LoadEcuFileVehiclesAsync();
    }

    private async void OnImportEcuFileClick(object sender, RoutedEventArgs e)
    {
        EcuFileMessage.Text = string.Empty;

        if (EcuFileVehicleSelector.SelectedItem is not VehicleSelectionItem selectedVehicle)
        {
            EcuFileMessage.Text = _localizer.Text("EcuFiles.NoVehicle");
            return;
        }

        if (string.IsNullOrWhiteSpace(EcuFileSourceTextBox.Text))
        {
            EcuFileMessage.Text = _localizer.Text("EcuFiles.NoFile");
            return;
        }

        var request = new EcuFileImportRequest(
            selectedVehicle.Id,
            EcuInfoId: null,
            EcuFileSourceTextBox.Text,
            _configuration.BackupDirectory,
            EcuFileTypeSelector.SelectedItem is EcuFileType fileType ? fileType : EcuFileType.Unknown,
            EmptyToNull(EcuFileOriginTextBox.Text),
            EmptyToNull(EcuFileReadMethodTextBox.Text),
            EmptyToNull(EcuFileProgrammerTextBox.Text),
            EmptyToNull(EcuFileNotesTextBox.Text));

        var result = await _ecuFileImportService.ImportAsync(request);
        EcuFileMessage.Text = result.IsSuccess
            ? _localizer.Text("EcuFiles.ImportSuccess")
            : $"{_localizer.Text("EcuFiles.ImportFailure")} {result.ErrorMessage}";

        if (result.IsSuccess)
        {
            await LoadEcuFilesForSelectedVehicleAsync();
        }
    }

    private async void OnDeleteEcuFileClick(object sender, RoutedEventArgs e)
    {
        EcuFileMessage.Text = string.Empty;
        if (EcuFilesGrid.SelectedItem is not EcuFileListItem selectedFile)
        {
            EcuFileMessage.Text = _localizer.Text("EcuFiles.NoSelection");
            return;
        }

        if (!ConfirmDelete())
        {
            return;
        }

        var result = await _ecuFileService.DeleteAsync(selectedFile.Id);
        EcuFileMessage.Text = result.IsSuccess
            ? _localizer.Text("EcuFiles.DeleteSuccess")
            : $"{_localizer.Text("EcuFiles.DeleteFailure")} {result.ErrorMessage}";
        if (result.IsSuccess)
        {
            await LoadEcuFilesForSelectedVehicleAsync();
        }
    }

    private void LoadProgrammerCapabilities()
    {
        ProgrammersGrid.ItemsSource = _programmerCapabilityService.GetCapabilities()
            .Select(capability => new ProgrammerCapabilityListItem(
                capability.Name,
                capability.ConnectionType,
                capability.SupportedMode.ToString(),
                FormatBoolean(capability.DirectReadSupported),
                FormatBoolean(capability.DirectWriteSupported),
                FormatBoolean(capability.RequiresExternalSoftware),
                capability.Notes))
            .ToArray();
    }

    private async Task LoadVehiclesAsync()
    {
        try
        {
            var vehicles = await _vehicleService.ListAsync();
            VehiclesGrid.ItemsSource = vehicles
                .Select(vehicle => new VehicleListItem(
                    vehicle.Id,
                    vehicle.Make,
                    vehicle.Model,
                    vehicle.EngineCode,
                    vehicle.Year))
                .ToArray();
            VehiclesEmptyText.Visibility = vehicles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_selectedVehicleId is null)
            {
                StartNewVehicle();
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to load vehicles.", exception);
            VehicleMessage.Text = _localizer.Text("Vehicles.LoadFailure");
        }
    }

    private async void OnVehicleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingVehicle || VehiclesGrid.SelectedItem is not VehicleListItem selectedVehicle)
        {
            return;
        }

        var vehicle = await _vehicleService.GetByIdAsync(selectedVehicle.Id);
        if (vehicle is null)
        {
            return;
        }

        LoadVehicleIntoForm(vehicle);
    }

    private void OnNewVehicleClick(object sender, RoutedEventArgs e) => StartNewVehicle();

    private async void OnRefreshVehiclesClick(object sender, RoutedEventArgs e) => await LoadVehiclesAsync();

    private async void OnSaveVehicleClick(object sender, RoutedEventArgs e)
    {
        VehicleMessage.Text = string.Empty;

        if (!TryBuildVehicleFromForm(out var vehicle, out var errorMessage))
        {
            VehicleMessage.Text = errorMessage;
            return;
        }

        var result = _selectedVehicleId is null
            ? await _vehicleService.CreateAsync(vehicle)
            : await _vehicleService.UpdateAsync(vehicle);

        VehicleMessage.Text = result.IsSuccess
            ? _localizer.Text("Vehicles.SaveSuccess")
            : $"{_localizer.Text("Vehicles.SaveFailure")} {result.ErrorMessage}";

        if (result.IsSuccess && result.Value is not null)
        {
            _selectedVehicleId = result.Value.Id;
            await LoadVehiclesAsync();
        }
    }

    private async void OnDeleteVehicleClick(object sender, RoutedEventArgs e)
    {
        VehicleMessage.Text = string.Empty;
        if (_selectedVehicleId is null)
        {
            VehicleMessage.Text = _localizer.Text("Vehicles.NoSelection");
            return;
        }

        if (!ConfirmDelete())
        {
            return;
        }

        var result = await _vehicleService.DeleteAsync(_selectedVehicleId.Value);
        VehicleMessage.Text = result.IsSuccess
            ? _localizer.Text("Vehicles.DeleteSuccess")
            : $"{_localizer.Text("Vehicles.DeleteFailure")} {result.ErrorMessage}";
        if (result.IsSuccess)
        {
            _selectedVehicleId = null;
            await LoadVehiclesAsync();
        }
    }

    private void OnVehicleProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingVehicle || VehicleProfileSelector.SelectedItem is not VehicleProfileOption { Profile: { } profile })
        {
            return;
        }

        VehicleMakeTextBox.Text = profile.Make;
        VehicleModelTextBox.Text = profile.Model;
        VehicleEngineTextBox.Text = profile.Engine;
        VehicleEngineCodeTextBox.Text = profile.EngineCode;
        VehicleFuelSelector.SelectedItem = profile.FuelType;
        VehicleNotesTextBox.Text = profile.Notes;
    }

    private void StartNewVehicle()
    {
        _isLoadingVehicle = true;
        _selectedVehicleId = null;
        VehiclesGrid.SelectedItem = null;
        VehicleProfileSelector.SelectedIndex = 0;
        VehicleMakeTextBox.Text = string.Empty;
        VehicleModelTextBox.Text = string.Empty;
        VehicleYearTextBox.Text = string.Empty;
        VehicleFuelSelector.SelectedItem = FuelType.Unknown;
        VehicleEngineTextBox.Text = string.Empty;
        VehicleEngineCodeTextBox.Text = string.Empty;
        VehicleVinTextBox.Text = string.Empty;
        VehicleLicensePlateTextBox.Text = string.Empty;
        VehicleNotesTextBox.Text = string.Empty;
        VehicleMessage.Text = string.Empty;
        _isLoadingVehicle = false;
        RefreshLocalizedText();
    }

    private void LoadVehicleIntoForm(Vehicle vehicle)
    {
        _isLoadingVehicle = true;
        _selectedVehicleId = vehicle.Id;
        VehicleProfileSelector.SelectedIndex = 0;
        VehicleMakeTextBox.Text = vehicle.Make;
        VehicleModelTextBox.Text = vehicle.Model;
        VehicleYearTextBox.Text = vehicle.Year?.ToString() ?? string.Empty;
        VehicleFuelSelector.SelectedItem = vehicle.FuelType;
        VehicleEngineTextBox.Text = vehicle.Engine;
        VehicleEngineCodeTextBox.Text = vehicle.EngineCode;
        VehicleVinTextBox.Text = vehicle.Vin;
        VehicleLicensePlateTextBox.Text = vehicle.LicensePlate;
        VehicleNotesTextBox.Text = vehicle.Notes;
        VehicleMessage.Text = string.Empty;
        _isLoadingVehicle = false;
        RefreshLocalizedText();
    }

    private bool TryBuildVehicleFromForm(out Vehicle vehicle, out string errorMessage)
    {
        vehicle = new Vehicle();
        errorMessage = string.Empty;

        int? year = null;
        if (!string.IsNullOrWhiteSpace(VehicleYearTextBox.Text))
        {
            if (!int.TryParse(VehicleYearTextBox.Text.Trim(), out var parsedYear))
            {
                errorMessage = _localizer.Text("Vehicles.InvalidYear");
                return false;
            }

            year = parsedYear;
        }

        vehicle = new Vehicle
        {
            Id = _selectedVehicleId ?? Guid.NewGuid(),
            Make = VehicleMakeTextBox.Text.Trim(),
            Model = VehicleModelTextBox.Text.Trim(),
            Year = year,
            FuelType = VehicleFuelSelector.SelectedItem is FuelType fuelType ? fuelType : FuelType.Unknown,
            Engine = VehicleEngineTextBox.Text.Trim(),
            EngineCode = VehicleEngineCodeTextBox.Text.Trim(),
            Vin = EmptyToNull(VehicleVinTextBox.Text),
            LicensePlate = EmptyToNull(VehicleLicensePlateTextBox.Text),
            Notes = EmptyToNull(VehicleNotesTextBox.Text)
        };

        return true;
    }

    private IReadOnlyList<VehicleProfileOption> BuildProfileOptions()
    {
        var options = new List<VehicleProfileOption>
        {
            new(_localizer.Text("Vehicles.Profile.None"), null)
        };

        options.AddRange(_vehicleProfileCatalog.Profiles.Select(profile => new VehicleProfileOption(profile.DisplayName, profile)));
        return options;
    }

    private IReadOnlyList<CalibrationProfileOption> BuildCalibrationProfileOptions() =>
        _calibrationProfileCatalog
            .ListAll()
            .Select(profile => new CalibrationProfileOption(profile.Key, profile.DisplayName))
            .ToArray();

    private IReadOnlyList<MapEditOperationOption> BuildMapEditOperationOptions() =>
    [
        new(_localizer.Text("MapWorkspace.EditOperation.Percentage"), EcuProjectMapEditOperation.ApplyPercentage),
        new(_localizer.Text("MapWorkspace.EditOperation.Increment"), EcuProjectMapEditOperation.IncrementAbsolute),
        new(_localizer.Text("MapWorkspace.EditOperation.Decrement"), EcuProjectMapEditOperation.DecrementAbsolute),
        new(_localizer.Text("MapWorkspace.EditOperation.Multiplier"), EcuProjectMapEditOperation.ApplyMultiplier),
        new(_localizer.Text("MapWorkspace.EditOperation.Set"), EcuProjectMapEditOperation.SetAbsolute)
    ];

    private void RefreshVehicleProfileOptions()
    {
        var selectedProfileId = (VehicleProfileSelector.SelectedItem as VehicleProfileOption)?.Profile?.Id;

        _isLoadingVehicle = true;
        var options = BuildProfileOptions();
        VehicleProfileSelector.ItemsSource = options;
        VehicleProfileSelector.SelectedItem = options.FirstOrDefault(option => option.Profile?.Id == selectedProfileId) ?? options[0];
        _isLoadingVehicle = false;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EcuFileSelectionItem ToEcuFileSelectionItem(EcuFile file) =>
        new(file.Id, $"{file.FileName} ({file.Sha256Hash[..12]})");

    private string FormatBoolean(bool value) =>
        value ? _localizer.Text("Common.Yes") : _localizer.Text("Common.No");

    private bool ConfirmDelete() =>
        MessageBox.Show(
            _localizer.Text("Common.DeleteConfirm"),
            _localizer.Text("Common.Delete"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
}

public sealed record VehicleListItem(Guid Id, string Make, string Model, string EngineCode, int? Year);

public sealed record ProjectListItem(
    Guid Id,
    string Name,
    string Vehicle,
    string Status,
    string NextStep,
    int VersionCount,
    int MapDefinitionCount);

public sealed record VehicleProfileOption(string DisplayName, VehicleProfile? Profile);

public sealed record VehicleSelectionItem(Guid Id, string DisplayName);

public sealed record EcuListItem(Guid Id, string Manufacturer, string EcuFamily, string SupportStatus);

public sealed record ProgrammerCapabilityListItem(
    string Name,
    string ConnectionType,
    string SupportedMode,
    string DirectRead,
    string DirectWrite,
    string RequiresExternalSoftware,
    string Notes);

public sealed record EcuFileListItem(
    Guid Id,
    string FileName,
    string FileType,
    long SizeBytes,
    string Sha256Hash,
    string ChecksumStatus,
    string ImportedAt);

public sealed record EcuFileSelectionItem(Guid Id, string DisplayName);

public sealed record ComparisonListItem(
    string OriginalFileId,
    string ModifiedFileId,
    long DifferenceCount,
    string PercentChanged,
    string DifferenceSummary,
    string DifferenceBlockSummary,
    string Result,
    string ComparedAt);

public sealed record ReportComparisonSelectionItem(Guid Id, string DisplayName);

public sealed record MapWorkspacePointListItem(
    string Offset,
    string OriginalHex,
    string ModifiedHex,
    string Difference,
    int RawValue,
    string ScaledValue);

public sealed record MapDefinitionSelectionItem(
    string MapId,
    string DisplayName,
    string Name,
    long StartOffset,
    int Length,
    int? RowCount,
    int? ColumnCount,
    string DataType);

public sealed record MapEditOperationOption(string DisplayName, EcuProjectMapEditOperation Operation);

public sealed record EcuSelectionItem(Guid Id, string DisplayName);

public sealed record CalibrationProfileOption(string Key, string DisplayName);
