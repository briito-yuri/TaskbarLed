using System.Text.Json.Nodes;

namespace TaskbarLed.Sources.ClaudeCode.Tests;

public sealed class ClaudeCodeHookInstallerTests : IDisposable
{
    private const string HookExe = @"C:\Program Files\TaskbarLed\TaskbarLed.Hook.exe";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"taskbarled-{Guid.NewGuid():N}");
    private readonly string _settings;
    private readonly ClaudeCodeHookInstaller _installer;

    public ClaudeCodeHookInstallerTests()
    {
        _settings = Path.Combine(_dir, "settings.json");
        _installer = new ClaudeCodeHookInstaller(_settings, HookExe);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Installs_into_a_missing_file()
    {
        _installer.Install();

        Assert.True(_installer.IsInstalled());
        var command = Read()["hooks"]!["Stop"]![0]!["hooks"]![0]!["command"]!.GetValue<string>();
        Assert.Equal($"\"{HookExe}\" claude-code", command);
    }

    [Fact]
    public void Keeps_existing_settings_and_user_hooks()
    {
        Write("""
            {
              "agentPushNotifEnabled": true,
              // comments are tolerated
              "hooks": { "Stop": [ { "hooks": [ { "type": "command", "command": "say done" } ] } ] },
            }
            """);

        _installer.Install();

        var root = Read();
        Assert.True(root["agentPushNotifEnabled"]!.GetValue<bool>());
        var stop = root["hooks"]!["Stop"]!.AsArray();
        Assert.Equal(2, stop.Count);
        Assert.Equal("say done", stop[0]!["hooks"]![0]!["command"]!.GetValue<string>());
        Assert.True(File.Exists(_settings + ".taskbarled.bak"));
    }

    [Fact]
    public void Installing_twice_changes_nothing()
    {
        _installer.Install();
        var first = File.ReadAllText(_settings);

        _installer.Install();

        Assert.Equal(first, File.ReadAllText(_settings));
    }

    [Fact]
    public void Reinstall_from_a_new_path_replaces_the_old_hook()
    {
        new ClaudeCodeHookInstaller(_settings, @"C:\old\TaskbarLed.Hook.exe").Install();

        _installer.Install();

        var stop = Read()["hooks"]!["Stop"]!.AsArray();
        Assert.Single(stop);
        Assert.True(_installer.IsInstalled());
    }

    [Fact]
    public void Uninstall_removes_only_our_hooks_and_empty_containers()
    {
        Write("""{"hooks":{"Stop":[{"hooks":[{"type":"command","command":"say done"}]}]}}""");
        _installer.Install();

        _installer.Uninstall();

        var hooks = Read()["hooks"]!.AsObject();
        Assert.Equal(["Stop"], hooks.Select(p => p.Key));
        Assert.Equal("say done", hooks["Stop"]![0]!["hooks"]![0]!["command"]!.GetValue<string>());
        Assert.False(_installer.IsInstalled());
    }

    [Fact]
    public void Uninstall_on_a_clean_file_drops_the_hooks_key()
    {
        Write("""{"model":"opus"}""");
        _installer.Install();

        _installer.Uninstall();

        Assert.Null(Read()["hooks"]);
        Assert.Equal("opus", Read()["model"]!.GetValue<string>());
    }

    [Fact]
    public void Malformed_settings_are_never_overwritten()
    {
        Write("{ this is not json");

        Assert.Throws<InvalidOperationException>(_installer.Install);
        Assert.Equal("{ this is not json", File.ReadAllText(_settings));
    }

    private void Write(string json)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_settings, json);
    }

    private JsonNode Read() => JsonNode.Parse(File.ReadAllText(_settings))!;
}
