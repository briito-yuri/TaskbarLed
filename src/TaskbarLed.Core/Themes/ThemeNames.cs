using System.Globalization;

namespace TaskbarLed.Core.Themes;

/// <summary>Localized names for the shared panel themes.</summary>
public static class ThemeNames
{
    public static string? Get(string id, CultureInfo culture)
    {
        var portuguese = culture.TwoLetterISOLanguageName == "pt";
        return id switch
        {
            "LiquidGlass" => "Liquid Glass",
            "AmberTree" => portuguese ? "Âmbar em árvore" : "Amber Tree",
            "CompactCharcoal" => portuguese ? "Carvão compacto" : "Compact Charcoal",
            "AmberRail" => portuguese ? "Âmbar com régua" : "Amber Rail",
            "ClearList" => portuguese ? "Lista Clara" : "Clear List",
            "EditorialIndex" => portuguese ? "Índice Editorial" : "Editorial Index",
            "CobaltWindow" => portuguese ? "Janela Cobalto" : "Cobalt Window",
            "VioletPhosphor" => portuguese ? "Fósforo Violeta" : "Violet Phosphor",
            "LineMap" => portuguese ? "Mapa de Linhas" : "Line Map",
            "OliveLcd" => portuguese ? "LCD Oliva" : "Olive LCD",
            _ => null,
        };
    }
}
