using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Presentation;

/// <summary>One session tab on the panel.</summary>
public sealed partial class ChannelViewModel : ObservableObject
{
    public string? SessionId { get; set; }

    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Glyph), nameof(IsRunning), nameof(IsCompleted), nameof(IsAwaiting))]
    public partial SessionStatus Status { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public bool IsRunning => Status == SessionStatus.Running;

    public bool IsCompleted => Status == SessionStatus.Completed;

    public bool IsAwaiting => Status == SessionStatus.AwaitingInput;

    /// <summary>Status marker after the label, readable even without color.</summary>
    public string Glyph => Status switch
    {
        SessionStatus.Running => "▸",
        SessionStatus.Completed => "✓",
        SessionStatus.AwaitingInput => "!",
        _ => "·",
    };
}

public sealed partial class SessionDisplayRowViewModel(SessionDisplayRow row, bool portuguese, bool uppercase, string themeId) : ObservableObject
{
    public SessionDisplayRow Row { get; } = row with { SecondsRemaining = 0 };
    public string Label => uppercase ? Row.Label.ToUpperInvariant() : Row.Label;

    public bool IsLineMap => themeId == "LineMap";
    public bool IsEditorial => themeId == "EditorialIndex";
    public bool IsViolet => themeId == "VioletPhosphor";
    public bool IsCobalt => themeId == "CobaltWindow";
    public bool ShowEditorialHeader => IsHeader && IsEditorial;
    public bool ShowVioletHeader => IsHeader && IsViolet;
    public bool HasSplitHeader => IsHeader && (IsEditorial || IsViolet);
    public string EngineLabel => Row.Engine?.ToUpperInvariant() ?? "";
    public string Heading => Row.Heading ?? Row.Label;
    public string EditorialHeading => $"→  {Heading}";
    public string StationMarker => !IsChild ? "" : themeId switch
    {
        "CompactCharcoal" or "CobaltWindow" => "↳  ",
        "AmberTree" or "AmberRail" or "OliveLcd" => Row.Status == SessionStatus.Completed ? "✓  " : "·  ",
        _ => "",
    };
    public string CompactLabel => themeId is "EditorialIndex" or "VioletPhosphor" or "CobaltWindow" or "OliveLcd" or "AmberTree" or "AmberRail" or "CompactCharcoal"
        ? StationMarker + (uppercase ? StationLabel.ToUpperInvariant() : StationLabel) : Label;
    public bool ConnectToNext => !Row.LastStation;
    public string StationLabel => Row.StationName ?? Row.Label;
    public string LineLetter => string.IsNullOrWhiteSpace(Row.Engine) ? "?" : StringInfo.GetNextTextElement(Row.Engine).ToUpperInvariant();
    public IBrush LineBrush => TransitColors.TryGetValue(LineLetter, out var brush) ? brush : TransitFallback;

    private static readonly Dictionary<string, IBrush> TransitColors = new(StringComparer.Ordinal)
    {
        ["A"] = new SolidColorBrush(Color.Parse("#A74232")),
        ["B"] = new SolidColorBrush(Color.Parse("#365B93")),
        ["C"] = new SolidColorBrush(Color.Parse("#146B74")),
        ["G"] = new SolidColorBrush(Color.Parse("#77572B")),
        ["K"] = new SolidColorBrush(Color.Parse("#6A4B7D")),
        ["Q"] = new SolidColorBrush(Color.Parse("#326343")),
        ["Z"] = new SolidColorBrush(Color.Parse("#664292")),
    };

    private static readonly IBrush TransitFallback = new SolidColorBrush(Color.Parse("#465D6B"));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Status))]
    public partial int SecondsRemaining { get; set; } = row.SecondsRemaining;
    public string? SessionId => Row.SessionId;
    public bool IsHeader => Row.IsHeader;
    public bool IsChild => !Row.IsHeader;
    public bool IsWaiting => Row.Status == SessionStatus.AwaitingInput;
    public bool IsDone => Row.Status == SessionStatus.Completed;
    public bool IsIdle => Row.Status == SessionStatus.Idle;

    public string Status => !Row.HasStatus ? "" : Row.Status switch
    {
        SessionStatus.Running => portuguese ? "▸ Rodando" : "▸ Running",
        SessionStatus.AwaitingInput => portuguese ? "! Aguardando" : "! Waiting",
        SessionStatus.Completed => $"{(portuguese ? "✓ Concluído" : "✓ Done")}{(SecondsRemaining > 0 ? $" · {SecondsRemaining}s" : "")}",
        _ => portuguese ? "Ocioso" : "Idle",
    };
}

public sealed record SessionDisplayGroupViewModel(SessionDisplayRowViewModel Header, IReadOnlyList<SessionDisplayRowViewModel> Children)
{
    public bool HasChildren => Children.Count > 0;
}

/// <summary>
/// State bound by the floating panel. Same contract as the WPF <c>PanelViewModel</c> (minus edit mode);
/// both apply a Core <see cref="PanelFrame"/>, so the composer stays the single source of truth.
/// </summary>
public sealed partial class PanelViewModel : ObservableObject
{
    private readonly CultureInfo _culture;
    private PanelFrame? _frame;

    public PanelViewModel(CultureInfo culture) => _culture = culture ?? throw new ArgumentNullException(nameof(culture));

    /// <summary>Tabs are updated in place so their blink animations do not restart every second.</summary>
    public ObservableCollection<ChannelViewModel> Channels { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSubagents), nameof(ShowSessionList), nameof(ShowClearList))]
    public partial IReadOnlyList<SessionDisplayRowViewModel> SessionRows { get; set; } = [];

    public bool HasSubagents => SessionRows.Count != 0;

    [ObservableProperty]
    public partial string Feed { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ElapsedText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedIsAttention), nameof(FeedIsRunning))]
    public partial SessionStatus SelectedStatus { get; set; }

    /// <summary>A session finished and the user has not opened Claude yet: the outline blinks green.</summary>
    [ObservableProperty]
    public partial bool IsBlinking { get; set; }

    /// <summary>A session waits for the user: the outline blinks amber.</summary>
    [ObservableProperty]
    public partial bool NeedsAttention { get; set; }

    /// <summary>Any session is running, finished-unseen or waiting.</summary>
    [ObservableProperty]
    public partial bool HasActivity { get; set; }

    /// <summary>Theme preference: LED themes shout in capitals.</summary>
    [ObservableProperty]
    public partial bool Uppercase { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsClearList), nameof(IsLcd), nameof(ShowSessionList), nameof(ShowClearList))]
    public partial string ThemeId { get; set; } = "ClaudeCode";

    public bool IsClearList => ThemeId == "ClearList";
    public bool ShowSessionList => HasSubagents && !IsClearList;
    public bool ShowClearList => HasSubagents && IsClearList;
    public bool IsLcd => ThemeId == "OliveLcd";

    [ObservableProperty]
    public partial IReadOnlyList<SessionDisplayGroupViewModel> SessionGroups { get; set; } = [];

    partial void OnThemeIdChanged(string value) => RefreshTheme();

    public bool FeedIsRunning => SelectedStatus == SessionStatus.Running;

    public bool FeedIsAttention => SelectedStatus == SessionStatus.AwaitingInput;

    public void Apply(PanelFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _frame = frame;

        if (!SessionRows.Select(row => row.Row).SequenceEqual(frame.SessionRows.Select(row => row with { SecondsRemaining = 0 })))
        {
            SessionRows = [.. frame.SessionRows.Select(row =>
                new SessionDisplayRowViewModel(row, _culture.TwoLetterISOLanguageName == "pt", Uppercase, ThemeId))];
            var groups = new List<SessionDisplayGroupViewModel>();
            for (var i = 0; i < SessionRows.Count;)
            {
                var header = SessionRows[i++];
                var children = new List<SessionDisplayRowViewModel>();
                while (i < SessionRows.Count && !SessionRows[i].IsHeader)
                {
                    children.Add(SessionRows[i++]);
                }

                groups.Add(new SessionDisplayGroupViewModel(header, children));
            }

            SessionGroups = groups;
        }
        else
        {
            for (var i = 0; i < SessionRows.Count; i++)
            {
                SessionRows[i].SecondsRemaining = frame.SessionRows[i].SecondsRemaining;
            }
        }
        Feed = Cased(frame.Feed);
        ElapsedText = frame.ElapsedText;
        SelectedStatus = frame.SelectedStatus;
        IsBlinking = frame.HasUnseenCompletion;
        NeedsAttention = frame.NeedsAttention;
        HasActivity = frame.HasActivity;

        while (Channels.Count > frame.Channels.Count)
        {
            Channels.RemoveAt(Channels.Count - 1);
        }

        while (Channels.Count < frame.Channels.Count)
        {
            Channels.Add(new ChannelViewModel());
        }

        for (var i = 0; i < frame.Channels.Count; i++)
        {
            Channels[i].SessionId = frame.Channels[i].SessionId;
            Channels[i].Label = Cased(frame.Channels[i].Label);
            Channels[i].Status = frame.Channels[i].Status;
            Channels[i].IsSelected = frame.Channels[i].IsSelected;
        }
    }

    private string Cased(string text) => Uppercase ? text.ToUpper(_culture) : text;

    partial void OnUppercaseChanged(bool value) => RefreshTheme();

    private void RefreshTheme()
    {
        if (_frame is not null)
        {
            SessionRows = [];
            SessionGroups = [];
            Apply(_frame);
        }
    }
}
