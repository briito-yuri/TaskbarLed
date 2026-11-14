using System.Globalization;
using System.Text.Json;

namespace TaskbarLed.Sources.ClaudeCode.Tests;

public class ToolActivityFormatterTests
{
    private readonly ToolActivityFormatter _pt = new(ActivityStrings.Portuguese, CultureInfo.GetCultureInfo("pt-BR"));

    [Theory]
    [InlineData("Read", """{"file_path":"/home/a/README.md"}""", "Lendo README.md")]
    [InlineData("Write", """{"file_path":"C:\\x\\novo.cs"}""", "Criando novo.cs")]
    [InlineData("Bash", """{"command":"npm test","description":"Run unit tests"}""", "Rodando Run unit tests")]
    [InlineData("PowerShell", """{"command":"dotnet build\nmore"}""", "Rodando dotnet build")]
    [InlineData("Grep", """{"pattern":"TODO"}""", "Buscando TODO")]
    [InlineData("WebFetch", """{"url":"https://learn.microsoft.com/dotnet"}""", "Abrindo learn.microsoft.com")]
    [InlineData("Agent", """{"description":"Explore repo"}""", "Delegando: Explore repo")]
    [InlineData("TodoWrite", "{}", "Atualizando plano")]
    [InlineData("mcp__1a59c906__create_doc", "{}", "Usando create doc")]
    [InlineData("SomeNewTool", "{}", "Usando SomeNewTool")]
    [InlineData("Edit", "{}", "Editando …")]
    public void Describes_tools(string tool, string input, string expected)
    {
        using var document = JsonDocument.Parse(input);
        Assert.Equal(expected, _pt.Describe(tool, document.RootElement));
    }

    [Fact]
    public void Long_subjects_are_shortened()
    {
        using var document = JsonDocument.Parse($$"""{"command":"{{new string('x', 200)}}"}""");
        var text = _pt.Describe("Bash", document.RootElement);
        Assert.True(text.Length < 80);
        Assert.EndsWith("…", text, StringComparison.Ordinal);
    }
}
