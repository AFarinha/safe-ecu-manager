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
                ["Dashboard.Body"] = "Safe ECU Calibration Manager is in Phase 1: application foundation.\n\nThis delivery creates the desktop shell, main navigation, local logging, global error handling and base configuration.\n\nDangerous capabilities remain disabled: ECU writing, flashing, direct Galletto 1260 control, real map modification and any technical bypass.",
                ["Section.FutureBody"] = "This section already exists in the navigation to stabilize the application structure, but its functionality will be implemented in its own phase.\n\nUntil validated support exists, the expected behavior is to block or mark the feature as NotSupported.",
                ["Nav.Dashboard.Title"] = "Dashboard",
                ["Nav.Dashboard.Description"] = "Application status and safe next steps.",
                ["Nav.Vehicles.Title"] = "Vehicles",
                ["Nav.Vehicles.Description"] = "Vehicle management. Planned for Phase 3.",
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
                ["Dashboard.Body"] = "Safe ECU Calibration Manager esta na Fase 1: base da aplicacao.\n\nEsta entrega cria a shell desktop, a navegacao principal, logging local, tratamento global de erros e configuracao base.\n\nFuncionalidades perigosas permanecem desativadas: escrita ECU, flashing, controlo direto do Galletto 1260, alteracao real de mapas e qualquer bypass tecnico.",
                ["Section.FutureBody"] = "Esta seccao ja existe na navegacao para estabilizar a estrutura da aplicacao, mas a funcionalidade sera implementada numa fase propria.\n\nEnquanto nao existir suporte validado, o comportamento esperado e bloquear ou marcar como NotSupported.",
                ["Nav.Dashboard.Title"] = "Dashboard",
                ["Nav.Dashboard.Description"] = "Estado geral da aplicacao e proximos passos seguros.",
                ["Nav.Vehicles.Title"] = "Veiculos",
                ["Nav.Vehicles.Description"] = "Gestao de veiculos. Sera implementado na Fase 3.",
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
