using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SafeEcu.Calibration;
using SafeEcu.Application.Calibrations;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.Navigation;
using SafeEcu.Application.Programmers;
using SafeEcu.Application.Reports;
using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
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
        var isVehicles = area == ApplicationArea.Vehicles;
        var isEcus = area == ApplicationArea.Ecus;
        var isEcuFiles = area == ApplicationArea.EcuFiles;
        var isComparison = area == ApplicationArea.Comparison;
        var isReports = area == ApplicationArea.Reports;
        var isProgrammers = area == ApplicationArea.Programmers;
        GenericPanel.Visibility = isVehicles || isEcus || isEcuFiles || isComparison || isReports || isProgrammers ? Visibility.Collapsed : Visibility.Visible;
        VehiclesPanel.Visibility = isVehicles ? Visibility.Visible : Visibility.Collapsed;
        EcusPanel.Visibility = isEcus ? Visibility.Visible : Visibility.Collapsed;
        EcuFilesPanel.Visibility = isEcuFiles ? Visibility.Visible : Visibility.Collapsed;
        ComparisonPanel.Visibility = isComparison ? Visibility.Visible : Visibility.Collapsed;
        ReportsPanel.Visibility = isReports ? Visibility.Visible : Visibility.Collapsed;
        ProgrammersPanel.Visibility = isProgrammers ? Visibility.Visible : Visibility.Collapsed;

        if (isVehicles)
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
        RefreshVehicleProfileOptions();
        NewVehicleButton.Content = _localizer.Text("Vehicles.New");
        SaveVehicleButton.Content = _localizer.Text("Vehicles.Save");
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
        ComparisonResultColumn.Header = _localizer.Text("Comparison.Result");
        ComparisonComparedAtColumn.Header = _localizer.Text("Comparison.ComparedAt");
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
        else if (_currentArea == ApplicationArea.Comparison)
        {
            _ = LoadComparisonResultsAsync();
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
}

public sealed record VehicleListItem(Guid Id, string Make, string Model, string EngineCode, int? Year);

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
    string Result,
    string ComparedAt);

public sealed record ReportComparisonSelectionItem(Guid Id, string DisplayName);
