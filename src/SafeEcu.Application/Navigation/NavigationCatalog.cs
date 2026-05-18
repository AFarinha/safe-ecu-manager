using SafeEcu.Domain.Application;

namespace SafeEcu.Application.Navigation;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationSection> Sections { get; } =
    [
        new(ApplicationArea.Dashboard, "Dashboard", "Estado geral da aplicação e próximos passos seguros.", true),
        new(ApplicationArea.Vehicles, "Veiculos", "Gestao de veiculos. Sera implementado na Fase 3.", false),
        new(ApplicationArea.Ecus, "ECUs/EDUs", "Identificacao manual/assistida de unidades. Ainda nao implementado.", false),
        new(ApplicationArea.EcuFiles, "Ficheiros ECU", "Importacao e validacao de ficheiros ECU. Ainda nao implementado.", false),
        new(ApplicationArea.Programmers, "Programadores", "Fluxos FileOnly e Galletto 1260 manual. Ainda nao implementado.", false),
        new(ApplicationArea.Comparison, "Comparacao", "Comparacao binaria entre ficheiros. Ainda nao implementado.", false),
        new(ApplicationArea.Reports, "Relatorios", "Relatorios tecnicos HTML. Ainda nao implementado.", false),
        new(ApplicationArea.Audit, "Auditoria", "Historico auditavel de acoes. Ainda nao implementado.", false),
        new(ApplicationArea.Settings, "Definicoes", "Configuracao local e limites de seguranca. Ainda nao implementado.", false)
    ];
}
