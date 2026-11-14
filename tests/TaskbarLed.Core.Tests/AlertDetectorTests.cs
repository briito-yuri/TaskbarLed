using System.Globalization;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;
using static TaskbarLed.Core.Tests.Build;

namespace TaskbarLed.Core.Tests;

public class AlertDetectorTests
{
    private readonly AlertDetector _detector = new(TickerStrings.Portuguese, CultureInfo.GetCultureInfo("pt-BR"));

    [Fact]
    public void Sessions_seen_for_the_first_time_do_not_alert()
    {
        Assert.Empty(_detector.Detect([Session("a", SessionStatus.Completed)], At(0)));
    }

    [Fact]
    public void Finishing_a_known_session_alerts_once_with_name_and_duration()
    {
        var running = Session("a", SessionStatus.Running) with { ProjectName = "api", PromptStartedAt = At(0) };
        _detector.Detect([running], At(0));

        var done = running with { Status = SessionStatus.Completed, PromptEndedAt = At(767) };
        var alert = Assert.Single(_detector.Detect([done], At(800)));

        Assert.Equal("1 api  ›  Concluído em 12m47s", alert.Text);
        Assert.Equal(SessionStatus.Completed, alert.Status);
        Assert.Empty(_detector.Detect([done], At(801)));
    }

    [Fact]
    public void Waiting_for_the_user_alerts()
    {
        var running = Session("a", SessionStatus.Running) with { ProjectName = "site" };
        _detector.Detect([running], At(0));

        var alert = Assert.Single(_detector.Detect([running with { Status = SessionStatus.AwaitingInput }], At(1)));

        Assert.Equal("1 site  ›  Aguardando você", alert.Text);
    }

    [Fact]
    public void Going_back_to_running_or_idle_does_not_alert()
    {
        var waiting = Session("a", SessionStatus.AwaitingInput);
        _detector.Detect([waiting], At(0));

        Assert.Empty(_detector.Detect([waiting with { Status = SessionStatus.Running }], At(1)));
        Assert.Empty(_detector.Detect([waiting with { Status = SessionStatus.Idle }], At(2)));
    }
}
