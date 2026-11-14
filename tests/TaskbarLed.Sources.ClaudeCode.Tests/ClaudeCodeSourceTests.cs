using System.Globalization;
using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.Sources.ClaudeCode.Tests;

public class ClaudeCodeSourceTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 14, 3, 28, 0, TimeSpan.Zero);
    private readonly FakeUsageReader _usage = new();
    private readonly ClaudeCodeSource _source;

    public ClaudeCodeSourceTests()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        _source = new ClaudeCodeSource(new ToolActivityFormatter(ActivityStrings.English, culture), _usage);
    }

    [Fact]
    public async Task UserPromptSubmit_becomes_PromptSubmitted_with_title_and_project()
    {
        var events = await Translate("""
            {"session_id":"s1","hook_event_name":"UserPromptSubmit","cwd":"C:\\Projetos\\TaskbarLed","prompt":"hi","session_title":"LED panel"}
            """);

        var prompt = Assert.IsType<PromptSubmitted>(Assert.Single(events));
        Assert.Equal("s1", prompt.SessionId);
        Assert.Equal("LED panel", prompt.Title);
        Assert.Equal("TaskbarLed", prompt.ProjectName);
        Assert.Equal(At, prompt.Timestamp);
    }

    [Fact]
    public async Task PreToolUse_describes_the_tool()
    {
        var events = await Translate("""
            {"session_id":"s1","hook_event_name":"PreToolUse","tool_name":"Edit","tool_input":{"file_path":"C:\\src\\scraper.py"}}
            """);

        Assert.Equal("Editing scraper.py", Assert.IsType<ActivityStarted>(Assert.Single(events)).Activity);
    }

    [Theory]
    [InlineData("AskUserQuestion")]
    [InlineData("ExitPlanMode")]
    public async Task Tools_that_ask_the_user_request_attention(string tool)
    {
        var events = await Translate("""{"session_id":"s1","hook_event_name":"PreToolUse","tool_input":{},"tool_name":""" + $"\"{tool}\"}}");

        Assert.IsType<AttentionRequested>(Assert.Single(events));
    }

    [Fact]
    public async Task Stop_completes_with_the_last_message_and_measures_context()
    {
        _usage.Value = 0.37;

        var events = await Translate("""
            {"session_id":"s1","hook_event_name":"Stop","transcript_path":"C:\\t.jsonl","last_assistant_message":"Done ✓"}
            """);

        Assert.Equal(2, events.Count);
        Assert.Equal("Done ✓", Assert.IsType<ResponseCompleted>(events[0]).LastMessage);
        Assert.Equal(0.37, Assert.IsType<ContextUsageMeasured>(events[1]).Usage);
        Assert.Equal("C:\\t.jsonl", _usage.LastPath);
    }

    [Fact]
    public async Task Permission_notification_requests_attention()
    {
        var events = await Translate("""
            {"session_id":"s1","hook_event_name":"Notification","notification_type":"permission_prompt","message":"Claude needs your permission to use Bash"}
            """);

        Assert.Equal("Claude needs your permission to use Bash", Assert.IsType<AttentionRequested>(Assert.Single(events)).Reason);
    }

    [Theory]
    [InlineData("""{"session_id":"s1","hook_event_name":"Notification","notification_type":"idle_prompt","message":"x"}""")]
    [InlineData("""{"session_id":"s1","hook_event_name":"Notification","message":"Claude is waiting for your input"}""")]
    public async Task Idle_reminders_are_ignored(string json)
    {
        Assert.Empty(await Translate(json));
    }

    [Theory]
    [InlineData("""{"hook_event_name":"Stop"}""")]
    [InlineData("""{"session_id":"s1"}""")]
    [InlineData("""{"session_id":"s1","hook_event_name":"SomethingNew"}""")]
    [InlineData("[]")]
    public async Task Unknown_or_incomplete_payloads_produce_nothing(string json)
    {
        Assert.Empty(await Translate(json));
    }

    [Fact]
    public async Task SessionEnd_ends_the_session()
    {
        var events = await Translate("""{"session_id":"s1","hook_event_name":"SessionEnd"}""");
        Assert.IsType<SessionEnded>(Assert.Single(events));
    }

    private async Task<IReadOnlyList<SessionEvent>> Translate(string json)
    {
        using var document = JsonDocument.Parse(json);
        return await _source.TranslateAsync(document.RootElement, At, CancellationToken.None);
    }

    private sealed class FakeUsageReader : ITranscriptUsageReader
    {
        public double? Value { get; set; }

        public string? LastPath { get; private set; }

        public ValueTask<double?> ReadContextUsageAsync(string transcriptPath, CancellationToken cancellationToken)
        {
            LastPath = transcriptPath;
            return ValueTask.FromResult(Value);
        }
    }
}
