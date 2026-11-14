using System.Globalization;

namespace TaskbarLed.Sources.ClaudeCode;

/// <summary>Localizable phrases for tool activity. Each takes the subject as <c>{0}</c>.</summary>
public sealed record ActivityStrings(
    string Editing,
    string Writing,
    string Reading,
    string Running,
    string Searching,
    string Browsing,
    string WebSearching,
    string Delegating,
    string Planning,
    string Using,
    string AskingYou,
    string ApprovingPlan)
{
    public static ActivityStrings English { get; } = new(
        Editing: "Editing {0}",
        Writing: "Writing {0}",
        Reading: "Reading {0}",
        Running: "Running {0}",
        Searching: "Searching {0}",
        Browsing: "Opening {0}",
        WebSearching: "Searching the web: {0}",
        Delegating: "Delegating: {0}",
        Planning: "Updating plan",
        Using: "Using {0}",
        AskingYou: "Question for you",
        ApprovingPlan: "Plan waiting for approval");

    public static ActivityStrings Portuguese { get; } = new(
        Editing: "Editando {0}",
        Writing: "Criando {0}",
        Reading: "Lendo {0}",
        Running: "Rodando {0}",
        Searching: "Buscando {0}",
        Browsing: "Abrindo {0}",
        WebSearching: "Pesquisando: {0}",
        Delegating: "Delegando: {0}",
        Planning: "Atualizando plano",
        Using: "Usando {0}",
        AskingYou: "Pergunta para você",
        ApprovingPlan: "Plano aguardando aprovação");

    public static ActivityStrings For(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return culture.TwoLetterISOLanguageName == "pt" ? Portuguese : English;
    }
}
