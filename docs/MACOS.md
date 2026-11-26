# TaskbarLed on macOS

The fork's macOS build targets Apple Silicon (M1 and newer) and requires macOS 14 Sonoma or later. The bundle includes the .NET 10 runtime for both the panel and hook. [Microsoft's .NET compatibility table](https://learn.microsoft.com/en-us/dotnet/core/install/macos#supported-versions)

## Build and connect Claude Code

From the repository root, with the .NET SDK from `global.json` and Apple's command-line tools:

```sh
src/TaskbarLed.App.Avalonia/build-macos.sh
```

The script produces `artifacts/TaskbarLed-osx-arm64/TaskbarLed.app` and `TaskbarLed-osx-arm64.zip` in the same directory. Move the app to its permanent location before registering hooks.

```sh
app="$PWD/artifacts/TaskbarLed-osx-arm64/TaskbarLed.app"
"$app/Contents/MacOS/TaskbarLed" --install-hooks
open "$app"
```

The installer merges hooks into the Claude Code user settings and saves a backup. Restart existing Claude Code sessions to load the hooks. Claude Code in a terminal, IDE or Claude Desktop sends the same events when it loads those settings. For a custom configuration directory, pass `CLAUDE_CONFIG_DIR` when installing. Run `--uninstall-hooks` with the same executable to disconnect.

The panel floats above the Dock on the primary display. It stays visible across foreground app changes while a session is active. Amber means input is needed, green means a turn has finished, and running sessions show their activity and time. Scroll to rotate the feed. Click the feed or a completed tab to acknowledge its result. The menu bar icon can keep the panel visible when idle and quit the app.

## Automatic local detection

TaskbarLed detects known executables for the signed-in user, including Claude, Codex, Cursor, OpenCode, Gemini CLI, Ollama and LM Studio. It checks about every two seconds, groups multiple processes for the same tool, and shows detected tools even without hooks. A detected process can be idle or running in the background. Hooks add per-session activity, requests for input and results.

To recognize a custom tool or orchestrator, add its executable filename and display name to `~/Library/Application Support/TaskbarLed/agents.json`. Changes apply on the next scan. For example:

```json
{
  "my-ai-runner": "My AI Runner"
}
```

The scan reads executable names, so applications hosted under generic processes like a browser or an interpreter, and tools on another machine, may need a local event adapter.

## Connect an orchestrator

Claude Code processes launched by an orchestrator are covered when they run on the Mac and load the installed hooks. For other agents, send one JSON object on stdin to `TaskbarLed.Hook agent` on the Mac. Emit events from the orchestrator's lifecycle callbacks, using a stable agent name and a distinct session ID per concurrent job. Use `started` for a new turn, `running` for progress or to resume after waiting, `waiting` for input, `done` for a result, and `ended` to remove a session. `title` and `message` are optional display text. Supply the same title on subsequent events if you want to keep a custom label.

```sh
hook="$app/Contents/MacOS/TaskbarLed.Hook"
printf '%s\n' '{"agent":"pipeline","session_id":"job-42","status":"started","title":"Build"}' | "$hook" agent
printf '%s\n' '{"agent":"pipeline","session_id":"job-42","status":"waiting","title":"Build","message":"Approval required"}' | "$hook" agent
printf '%s\n' '{"agent":"pipeline","session_id":"job-42","status":"done","title":"Build","message":"Tests passed"}' | "$hook" agent
```

Cloud jobs and tools with no local hooks need an orchestrator or adapter on the Mac to relay their events. The panel cannot infer their state from an open browser tab or process name. The bridge writes events to a per-user spool and the panel consumes them locally. The hook drops input larger than 2 MiB or events received once the queue reaches 500 files.

## Differences from Windows

The Mac build uses the existing LED theme and a floating panel. The Windows theme picker, placement editor, sounds, fullscreen edge alerts, automatic foreground-window matching, click-to-open, and login startup controls are unavailable in this build. The floating window requests visibility across Spaces, but fullscreen behavior depends on macOS and the foreground app. The build is ad hoc signed for local use. Distribution outside this Mac may require Developer ID signing and notarization.

See also [Windows documentation](../README.md).
