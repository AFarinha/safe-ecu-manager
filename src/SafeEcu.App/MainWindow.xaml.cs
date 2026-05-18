using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.Navigation;
using SafeEcu.Application.Programmers;
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
    private readonly VehicleProfileCatalog _vehicleProfileCatalog;
    private readonly ProgrammerCapabilityService _programmerCapabilityService;
    private readonly EcuFileImportService _ecuFileImportService;
    private readonly EcuFileService _ecuFileService;
    private ApplicationArea _currentArea = ApplicationArea.Dashboard;
    private Guid? _selectedVehicleId;
    private bool _isLoadingVehicle;

    public MainWindow(
        IAppLogger logger,
        AppConfiguration configuration,
        ITextLocalizer localizer,
        VehicleService vehicleService,
        VehicleProfileCatalog vehicleProfileCatalog,
        ProgrammerCapabilityService programmerCapabilityService,
        EcuFileImportService ecuFileImportService,
        EcuFileService ecuFileService)
    {
        _logger = logger;
        _configuration = configuration;
        _localizer = localizer;
        _vehicleService = vehicleService;
        _vehicleProfileCatalog = vehicleProfileCatalog;
        _programmerCapabilityService = programmerCapabilityService;
        _ecuFileImportService = ecuFileImportService;
        _ecuFileService = ecuFileService;

        InitializeComponent();

        VehicleFuelSelector.ItemsSource = Enum.GetValues<FuelType>();
        VehicleFuelSelector.SelectedItem = FuelType.Unknown;
        VehicleProfileSelector.ItemsSource = BuildProfileOptions();
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
        var isEcuFiles = area == ApplicationArea.EcuFiles;
        var isProgrammers = area == ApplicationArea.Programmers;
        GenericPanel.Visibility = isVehicles || isEcuFiles || isProgrammers ? Visibility.Collapsed : Visibility.Visible;
        VehiclesPanel.Visibility = isVehicles ? Visibility.Visible : Visibility.Collapsed;
        EcuFilesPanel.Visibility = isEcuFiles ? Visibility.Visible : Visibility.Collapsed;
        ProgrammersPanel.Visibility = isProgrammers ? Visibility.Visible : Visibility.Collapsed;

        if (isVehicles)
        {
            _ = LoadVehiclesAsync();
        }
        else if (isEcuFiles)
        {
            _ = LoadEcuFileVehiclesAsync();
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

    private string FormatBoolean(bool value) =>
        value ? _localizer.Text("Common.Yes") : _localizer.Text("Common.No");
}

public sealed record VehicleListItem(Guid Id, string Make, string Model, string EngineCode, int? Year);

public sealed record VehicleProfileOption(string DisplayName, VehicleProfile? Profile);

public sealed record VehicleSelectionItem(Guid Id, string DisplayName);

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
