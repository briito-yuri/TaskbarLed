namespace TaskbarLed.Core.Sessions;

/// <summary>Lifecycle state of an agent session, ordered by how much it needs the user.</summary>
public enum SessionStatus
{
    /// <summary>No prompt in flight and nothing waiting for the user.</summary>
    Idle = 0,

    /// <summary>The agent is working on a prompt.</summary>
    Running = 1,

    /// <summary>The agent finished and the user has not looked at it yet.</summary>
    Completed = 2,

    /// <summary>The agent is blocked on the user (permission request or question).</summary>
    AwaitingInput = 3,
}
