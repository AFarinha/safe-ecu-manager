using System.Windows;
using System.Windows.Controls;
using SafeEcu.Application.Common;
using SafeEcu.Application.Localization;
using SafeEcu.Application.Navigation;
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
    private ApplicationArea _currentArea = ApplicationArea.Dashboard;
    private Guid? _selectedVehicleId;
    private bool _isLoadingVehicle;

    public MainWindow(
        IAppLogger logger,
        AppConfiguration configuration,
        ITextLocalizer localizer,
        VehicleService vehicleService,
        VehicleProfileCatalog vehicleProfileCatalog)
    {
        _logger = logger;
        _configuration = configuration;
        _localizer = localizer;
        _vehicleService = vehicleService;
        _vehicleProfileCatalog = vehicleProfileCatalog;

        InitializeComponent();

        VehicleFuelSelector.ItemsSource = Enum.GetValues<FuelType>();
        VehicleFuelSelector.SelectedItem = FuelType.Unknown;
        VehicleProfileSelector.ItemsSource = BuildProfileOptions();

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
        GenericPanel.Visibility = area == ApplicationArea.Vehicles ? Visibility.Collapsed : Visibility.Visible;
        VehiclesPanel.Visibility = area == ApplicationArea.Vehicles ? Visibility.Visible : Visibility.Collapsed;

        if (area == ApplicationArea.Vehicles)
        {
            _ = LoadVehiclesAsync();
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
        NavigationItems.ItemsSource = NavigationCatalog.Sections
            .Select(section => new
            {
                section.Area,
                Title = _localizer.Text(section.TitleKey)
            })
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
}

public sealed record VehicleListItem(Guid Id, string Make, string Model, string EngineCode, int? Year);

public sealed record VehicleProfileOption(string DisplayName, VehicleProfile? Profile);
