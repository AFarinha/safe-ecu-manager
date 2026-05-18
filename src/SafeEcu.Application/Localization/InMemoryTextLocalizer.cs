namespace SafeEcu.Application.Localization;

public sealed class InMemoryTextLocalizer : ITextLocalizer
{
    private const string English = "en";
    private const string Portuguese = "pt";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Resources =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [English] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "Safe ECU Calibration Manager",
                ["App.DatabaseWarningTitle"] = "Local database",
                ["App.UnhandledError"] = "An unexpected error occurred. The application logged details locally.",
                ["App.ErrorTitle"] = "Error",
                ["Language.Label"] = "Language",
                ["Status.AvailableNow"] = "Available in this phase",
                ["Status.FuturePhase"] = "Prepared for a future phase",
                ["Footer.SafeMode"] = "Safe mode: FileOnly / ManualWorkflow. ECU writing and direct hardware control are disabled.",
                ["Dashboard.Body"] = "Safe ECU Calibration Manager is currently at Phase 3: vehicle management.\n\nThe application has a desktop shell, main navigation, local logging, global error handling, SQLite persistence and vehicle records with initial safe profiles.\n\nDangerous capabilities remain disabled: ECU writing, flashing, direct Galletto 1260 control, real map modification and any technical bypass.",
                ["Section.FutureBody"] = "This section already exists in the navigation to stabilize the application structure, but its functionality will be implemented in its own phase.\n\nUntil validated support exists, the expected behavior is to block or mark the feature as NotSupported.",
                ["Nav.Dashboard.Title"] = "Dashboard",
                ["Nav.Dashboard.Description"] = "Application status and safe next steps.",
                ["Nav.Vehicles.Title"] = "Vehicles",
                ["Nav.Vehicles.Description"] = "Create, edit and review vehicle records.",
                ["Vehicles.Profile"] = "Profile",
                ["Vehicles.Profile.None"] = "No profile",
                ["Vehicles.New"] = "New",
                ["Vehicles.Save"] = "Save",
                ["Vehicles.Refresh"] = "Refresh",
                ["Vehicles.Make"] = "Make",
                ["Vehicles.Model"] = "Model",
                ["Vehicles.Year"] = "Year",
                ["Vehicles.Engine"] = "Engine",
                ["Vehicles.EngineCode"] = "Engine code",
                ["Vehicles.Fuel"] = "Fuel",
                ["Vehicles.Vin"] = "VIN",
                ["Vehicles.LicensePlate"] = "License plate",
                ["Vehicles.Notes"] = "Notes",
                ["Vehicles.ListTitle"] = "Registered vehicles",
                ["Vehicles.FormTitle.New"] = "New vehicle",
                ["Vehicles.FormTitle.Edit"] = "Vehicle details",
                ["Vehicles.Empty"] = "No vehicles registered yet.",
                ["Vehicles.SaveSuccess"] = "Vehicle saved.",
                ["Vehicles.SaveFailure"] = "Vehicle could not be saved.",
                ["Vehicles.InvalidYear"] = "Year must be empty or a valid number.",
                ["Vehicles.LoadFailure"] = "Vehicles could not be loaded.",
                ["Nav.Ecus.Title"] = "ECUs/EDUs",
                ["Nav.Ecus.Description"] = "Manual or assisted unit identification. Not implemented yet.",
                ["Nav.EcuFiles.Title"] = "ECU Files",
                ["Nav.EcuFiles.Description"] = "ECU file import and validation. Not implemented yet.",
                ["Nav.Programmers.Title"] = "Programmers",
                ["Nav.Programmers.Description"] = "FileOnly and manual Galletto 1260 workflows. Not implemented yet.",
                ["Nav.Comparison.Title"] = "Comparison",
                ["Nav.Comparison.Description"] = "Binary comparison between files. Not implemented yet.",
                ["Nav.Reports.Title"] = "Reports",
                ["Nav.Reports.Description"] = "HTML technical reports. Not implemented yet.",
                ["Nav.Audit.Title"] = "Audit",
                ["Nav.Audit.Description"] = "Auditable action history. Not implemented yet.",
                ["Nav.Settings.Title"] = "Settings",
                ["Nav.Settings.Description"] = "Local configuration and safety limits. Not implemented yet."
            },
            [Portuguese] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "Safe ECU Calibration Manager",
                ["App.DatabaseWarningTitle"] = "Base de dados local",
                ["App.UnhandledError"] = "Ocorreu um erro inesperado. A aplicacao registou detalhes no log local.",
                ["App.ErrorTitle"] = "Erro",
                ["Language.Label"] = "Idioma",
                ["Status.AvailableNow"] = "Disponivel nesta fase",
                ["Status.FuturePhase"] = "Preparado para fase futura",
                ["Footer.SafeMode"] = "Modo seguro: FileOnly / ManualWorkflow. Escrita ECU e controlo direto de hardware estao desativados.",
                ["Dashboard.Body"] = "Safe ECU Calibration Manager esta atualmente na Fase 3: gestao de veiculos.\n\nA aplicacao ja tem shell desktop, navegacao principal, logging local, tratamento global de erros, persistencia SQLite e registos de veiculos com perfis seguros iniciais.\n\nFuncionalidades perigosas permanecem desativadas: escrita ECU, flashing, controlo direto do Galletto 1260, alteracao real de mapas e qualquer bypass tecnico.",
                ["Section.FutureBody"] = "Esta seccao ja existe na navegacao para estabilizar a estrutura da aplicacao, mas a funcionalidade sera implementada numa fase propria.\n\nEnquanto nao existir suporte validado, o comportamento esperado e bloquear ou marcar como NotSupported.",
                ["Nav.Dashboard.Title"] = "Dashboard",
                ["Nav.Dashboard.Description"] = "Estado geral da aplicacao e proximos passos seguros.",
                ["Nav.Vehicles.Title"] = "Veiculos",
                ["Nav.Vehicles.Description"] = "Criar, editar e consultar registos de veiculos.",
                ["Vehicles.Profile"] = "Perfil",
                ["Vehicles.Profile.None"] = "Sem perfil",
                ["Vehicles.New"] = "Novo",
                ["Vehicles.Save"] = "Guardar",
                ["Vehicles.Refresh"] = "Atualizar",
                ["Vehicles.Make"] = "Marca",
                ["Vehicles.Model"] = "Modelo",
                ["Vehicles.Year"] = "Ano",
                ["Vehicles.Engine"] = "Motor",
                ["Vehicles.EngineCode"] = "Codigo motor",
                ["Vehicles.Fuel"] = "Combustivel",
                ["Vehicles.Vin"] = "VIN",
                ["Vehicles.LicensePlate"] = "Matricula",
                ["Vehicles.Notes"] = "Notas",
                ["Vehicles.ListTitle"] = "Veiculos registados",
                ["Vehicles.FormTitle.New"] = "Novo veiculo",
                ["Vehicles.FormTitle.Edit"] = "Detalhes do veiculo",
                ["Vehicles.Empty"] = "Ainda nao existem veiculos registados.",
                ["Vehicles.SaveSuccess"] = "Veiculo guardado.",
                ["Vehicles.SaveFailure"] = "Nao foi possivel guardar o veiculo.",
                ["Vehicles.InvalidYear"] = "O ano deve estar vazio ou conter um numero valido.",
                ["Vehicles.LoadFailure"] = "Nao foi possivel carregar os veiculos.",
                ["Nav.Ecus.Title"] = "ECUs/EDUs",
                ["Nav.Ecus.Description"] = "Identificacao manual/assistida de unidades. Ainda nao implementado.",
                ["Nav.EcuFiles.Title"] = "Ficheiros ECU",
                ["Nav.EcuFiles.Description"] = "Importacao e validacao de ficheiros ECU. Ainda nao implementado.",
                ["Nav.Programmers.Title"] = "Programadores",
                ["Nav.Programmers.Description"] = "Fluxos FileOnly e Galletto 1260 manual. Ainda nao implementado.",
                ["Nav.Comparison.Title"] = "Comparacao",
                ["Nav.Comparison.Description"] = "Comparacao binaria entre ficheiros. Ainda nao implementado.",
                ["Nav.Reports.Title"] = "Relatorios",
                ["Nav.Reports.Description"] = "Relatorios tecnicos HTML. Ainda nao implementado.",
                ["Nav.Audit.Title"] = "Auditoria",
                ["Nav.Audit.Description"] = "Historico auditavel de acoes. Ainda nao implementado.",
                ["Nav.Settings.Title"] = "Definicoes",
                ["Nav.Settings.Description"] = "Configuracao local e limites de seguranca. Ainda nao implementado."
            }
        };

    private string _currentLanguageCode;

    public InMemoryTextLocalizer(string defaultLanguageCode = English)
    {
        _currentLanguageCode = Resources.ContainsKey(defaultLanguageCode) ? defaultLanguageCode : English;
    }

    public string CurrentLanguageCode => _currentLanguageCode;

    public IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new(English, "English"),
        new(Portuguese, "Portuguese")
    ];

    public void SetLanguage(string languageCode)
    {
        _currentLanguageCode = Resources.ContainsKey(languageCode) ? languageCode : English;
    }

    public string Text(string key)
    {
        if (Resources[_currentLanguageCode].TryGetValue(key, out var localizedText))
        {
            return localizedText;
        }

        return Resources[English].TryGetValue(key, out var fallbackText) ? fallbackText : key;
    }
}
