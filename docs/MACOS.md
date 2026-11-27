# TaskbarLed on macOS

The fork's macOS build targets Apple Silicon (M1 and newer) and requires macOS 14 Sonoma or later. The bundle includes the .NET 10 runtime for both the panel and hook. [Microsoft's .NET compatibility table](https://learn.microsoft.com/en-us/dotnet/core/install/macos#supported-versions)

## Install

[Download TaskbarLed for Apple Silicon](https://github.com/briito-yuri/TaskbarLed/releases/download/macos-v0.2.0/TaskbarLed-osx-arm64.zip). Unzip it, move `TaskbarLed.app` to Applications, and open it. The app and hook include the runtime. Process detection and Octo integration start when the app opens. Follow the steps below to enable Claude Code hooks as well.

The current release uses ad hoc signing. If macOS blocks the app on first open, see [Apple's instructions for opening an app that macOS cannot check](https://support.apple.com/102445).

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

By default, the panel floats above the Dock on the primary display. It stays visible across foreground app changes while a session is active. Amber means input is needed, green means a turn has finished, and running sessions show their activity and time. Scroll to rotate the feed. Click the feed or a completed tab to acknowledge its result. The menu bar icon can keep the panel visible when idle and quit the app.

## Move, resize and change the look

Right-click the panel or click the TaskbarLed menu bar icon. **Edit position and size** opens a preview you can drag to another spot. Drag the grip on the right edge to change the width. **Save** (Enter) stores the placement and snaps it to a nearby screen edge. **Cancel** (Esc) restores the previous placement. **Reset position above Dock** restores the default size and position.

Use **Theme** to switch live between Claude Code, Modern Terminal, Matrix, Mono, Minimal, Glass and Paper. The default is Claude Code. TaskbarLed stores the selected theme and placement in `~/Library/Application Support/TaskbarLed/panel.json`.

## Automatic local detection

TaskbarLed detects known executables for the signed-in user, including Octo, Antigravity (`agy`), Claude, Codex, Cursor, OpenCode, Gemini CLI, Ollama and LM Studio. It checks about every two seconds, groups multiple processes for the same tool, and shows detected tools even without hooks. A detected process can be idle or running in the background. Hooks add per-session activity, requests for input and results.

To recognize a custom tool or orchestrator, add its executable filename and display name to `~/Library/Application Support/TaskbarLed/agents.json`. Changes apply on the next scan. For example:

```json
{
  "my-ai-runner": "My AI Runner"
}
```

The scan reads executable names, so applications hosted under generic processes like a browser or an interpreter, and tools on another machine, may need a local event adapter.

## Octo

When Octo is running, TaskbarLed reads its local session status and shows working, paused for input and completed turns, including Antigravity sessions. Support uses Octo holder protocol v1, verified with Octo 0.2.52. The panel shows the engine Octo reports. For example, it displays Antigravity for a Gemini model running inside Antigravity.

## Active agents and subagents

The panel shows agent sessions and adds a scrollable row for subagents grouped by their parent conversation. Hover the row to read the entire list. `▸` means working, `!` means input is needed, and `✓` marks a reported completion. Click a completed tab to acknowledge it.

- **Antigravity** reads the local conversation index in read-only mode. It shows a worker even when the parent is idle and flags a transition to idle after the worker was observed running. It reads titles, agent names, parent links and status as available in the index.
- **Octo** includes child sessions with a parent link and uses the engine's reported turn status. This also works with Qwen or Kimi sessions Octo tracks.
- **Codex** reads recent local rollout records for parent links and turn starts, completions and cancellations. It scans up to 128 recently modified logs under `CODEX_HOME/sessions` (default `~/.codex/sessions`).
- **Claude Code** uses `SubagentStart` and `SubagentStop`. Run `--install-hooks` again and restart existing Claude Code sessions to load the added events.
- **Other tools or remote workers** can send local events using the bridge below. Pass `parent_session_id` with the parent's ID, and use the same `agent` value as the parent. A process without session telemetry appears as detected and supplies no subagent progress.

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

The Mac build uses a floating panel. Custom JSON themes, sounds, fullscreen edge alerts, automatic foreground-window matching, click-to-open, and login startup controls are unavailable in this build. The floating window requests visibility across Spaces, but fullscreen behavior depends on macOS and the foreground app. The build is ad hoc signed for local use. Distribution outside this Mac may require Developer ID signing and notarization.

See also [Windows documentation](../README.md).

## Publish a Mac release

Run the Mac test projects and the build script on an Apple Silicon Mac. Create a GitHub release for a new `macos-vX.Y.Z` tag on the tested commit and attach `artifacts/TaskbarLed-osx-arm64/TaskbarLed-osx-arm64.zip`. Update the download links in both READMEs and this guide to the new tag. Verify the public link downloads the same ZIP as the local build. For distribution with Gatekeeper verification, sign with Developer ID and notarize before uploading.
