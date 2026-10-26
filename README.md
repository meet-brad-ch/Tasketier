# Tasketier

**What:** A local-only Windows task tool. A global hotkey opens a one-line box; the task is typed
with inline marks (`@tomorrow`, `@15:30`, `!`, `#tag`) and saved on Enter. A list window shows
Today, Upcoming, Inbox and Done. A tray icon keeps it running. `tasketier.exe` exposes the same
store as a command line and as an MCP server, and a Claude Code plugin in this repo teaches an
LLM to use it.

**Why:** Tasks that come to mind during the day are lost because writing them down costs too much
attention. Capture has to take under two seconds from any window, and the daily look-through has
to show what is due, what is planned for today and what is done. An LLM should be able to add
and query tasks directly from a running session.

**Status:** working, 0.1.0 — capture box, list window with the four views, inline edit, plan
star, undo, settings, tray, autostart, CLI, MCP server, plugin, installer. 104 tests pass; every
gate of `scripts\verify.ps1` passes. Not yet done by hand on a second PC: the installer's runtime
download on a machine without .NET 10.

## How to run

```
dotnet build Tasketier.slnx
dotnet test  Tasketier.slnx
dotnet run   --project src\Tasketier.App                 # tray, hotkeys, the list window
dotnet run   --project src\Tasketier.App -- --capture    # the capture box
dotnet run   --project src\Tasketier.App -- --tray       # icon only, as at Windows start-up
dotnet run   --project src\Tasketier.Cli -- add "Buy milk @tomorrow #home"
dotnet run   --project src\Tasketier.Cli -- list --view today
dotnet run   --project src\Tasketier.Cli -- mcp          # the MCP server on stdin/stdout
.\scripts\verify.ps1
.\build-installer.ps1
```

**Verify:** `.\scripts\verify.ps1` — build with warnings as errors (analyzers, code style, nullable),
unit tests with coverage, `dotnet format` check, cyclomatic complexity (lizard, limit 9). Exits
non-zero on any FAIL or NOT VERIFIED.

**Prerequisites:** Windows 11 (10 1607 or later for the installed app), .NET SDK 10.0.400
(`net10.0` / `net10.0-windows`), git, lizard (`pip install lizard==1.17.31`) for the complexity
gate, Inno Setup 7 (`C:\Program Files\Inno Setup 7`) for the installer. `nuget.config` clears the
machine-level feeds, so the build is the same on every machine.

## Install

`.\build-installer.ps1` writes `out\installer\Tasketier-Setup-<version>.exe`. The installer puts
`TasketierApp.exe` (the tray app) and `tasketier.exe` (the CLI and MCP server) into
`C:\Program Files\Tasketier`, adds that folder to PATH, offers a start-with-Windows task, and
downloads the .NET Desktop Runtime 10.0 from Microsoft (SHA-256 checked) when it is missing.
Tasks and settings live in `%LOCALAPPDATA%\Tasketier` and survive an uninstall.

## Use

| Action | How |
| --- | --- |
| Add a task from anywhere | `Ctrl+Alt+A`, type, `Enter` (`Esc` cancels; focus returns to the previous window) |
| Open the list | `Ctrl+Alt+L`, or left-click the tray icon |
| Complete | the checkbox, or `Space` on the selected row |
| Plan for today without a date | the star (`☆` → `★`); it carries over until done or unstarred |
| Edit | double-click the title or `F2`; the whole line in inline syntax; `Enter` saves |
| Delete | `Del`; `Ctrl+Z` brings it back |
| Undo | `Ctrl+Z` (last 20 changes of the session, also captures) |
| Filter | type in the filter box (`Ctrl+F`), by title or `#tag` |
| Settings | tray menu: both hotkeys, the data folder, start with Windows |

Both hotkeys are changeable in Settings. When a chord is held by another program, the tray shows
a message and that hotkey stays off until it is changed.

**Inline syntax:** words are the title. `#tag` adds a tag. `!` is high, `!!` urgent (also as a
suffix: `Pay rent!!`). `@today`, `@tomorrow`, `@mon`…`@sun` (next such day, today counts),
`@2026-11-03`, `@+3d`, `@+2w` set the due day. `@15:30` sets the time, today when no day is given.
`@none` clears the date. An unknown `@` mark is an error, shown under the box before Enter.

**Views:** Today = overdue (first, red), due today, planned for today. Upcoming = due after
today, by day. Inbox = no date and not planned. Done = completed in the last 30 days.

**Command line:** `tasketier add|list|show|done|undone|edit|delete|plan|unplan|search|mcp|version`.
`--json` prints records for scripts; `--data <folder>` points at another `tasks.json`.
`tasketier help` shows everything.

## LLM access (MCP + plugin)

`tasketier mcp` is a stdio MCP server with the tools `add_task`, `list_tasks`, `get_task`,
`complete_task`, `reopen_task`, `update_task`, `delete_task`, `plan_today`, `search_tasks`. Every
tool returns the affected records as JSON; failures carry their message.

The Claude Code plugin in `plugin\` bundles a skill (when to use which tool, the inline syntax,
the working rules) and the server registration (`${ProgramFiles}\Tasketier\tasketier.exe mcp`).
Install it in Claude Code:

```
/plugin marketplace add meet-brad-ch/Tasketier     # or the local clone: /plugin marketplace add C:\Apps\Projects\Tasketier
/plugin install tasketier@tasketier
```

The app, the CLI and the server share one file under a cross-process lock; the app reloads when
the file changes, so a task added by Claude appears in the list at once.

## Storage

One JSON file, `%LOCALAPPDATA%\Tasketier\tasks.json` (folder changeable in Settings, for example
to a synced folder). Writes are atomic (temp file + replace). The first write of each day copies
the previous file to `backups\tasks-<date>.json`. A damaged file is reported with its path and
never replaced. No network, no telemetry.

## Layout

- `src/Tasketier.Core/` — engine-free library: task model, inline-syntax parser, JSON store
  (mutex, atomic replace, daily backup, file watcher), view queries, service, undo, settings.
- `src/Tasketier.App/` — WPF (`TasketierApp.exe`): capture window, list window, settings window,
  tray icon, global hotkeys, instance signals, autostart.
- `src/Tasketier.Cli/` — console (`tasketier.exe`): the commands and the `mcp` server
  (`ModelContextProtocol` 2.3.0).
- `plugin/` — the Claude Code plugin; `.claude-plugin/marketplace.json` at the root lists it.
- `tests/Tasketier.Core.Tests/`, `tests/Tasketier.Cli.Tests/` — xUnit; the CLI tests run the real
  exe as an MCP server with the SDK client.
- `installer/Tasketier.iss`, `build-installer.ps1` — the Inno Setup installer.
- `scripts/verify.ps1` — the quality gates. `tools/make_icon.py` — draws the icon.

## Not in the MVP

Due-time toasts, recurring tasks, notes per task (candidates for 0.2); projects, subtasks, sync,
mobile, calendar, attachments, time tracking, export.

## Decisions

One line each: the choice, and the reason it was made. Append; do not rewrite history.

- 2026-10-26 — WPF on .NET 10 over WinUI 3 / Avalonia / Python: existing tray, autostart and
  settings-file code to reuse; a single framework-dependent exe.
- 2026-10-26 — one JSON file over todo.txt or SQLite: human-readable, no dependency, hundreds
  of tasks at most; a cross-process mutex keeps app, CLI and MCP server from racing.
- 2026-10-26 — MVP boundary as listed above; toasts wait for 0.2 because forgetting is a
  capture problem first.
- 2026-10-26 — LLM access as stdio MCP + CLI in one exe over the shared store, not an HTTP
  server inside the app: works when the app is not running, no ASP.NET Core in a WPF app;
  a Claude Code plugin in the repo bundles the skill and the MCP registration.
- 2026-10-26 — default hotkeys `Ctrl+Alt+A` (capture) and `Ctrl+Alt+L` (list): the first choice,
  `Ctrl+Alt+Space` / `Ctrl+Alt+T`, is held by other programs on the owner's PC (measured with
  RegisterHotKey).
- 2026-10-26 — the app is `TasketierApp.exe`, the CLI `tasketier.exe`: both live in one folder and
  Windows file names are case-insensitive, so they cannot both be `Tasketier.exe`.
- 2026-10-26 — installed into `C:\Program Files\Tasketier` with elevation (not per user): the
  .NET runtime bootstrap needs it, as in PassKit; the plugin's `.mcp.json` uses `${ProgramFiles}`.
- 2026-10-26 — public repository under MIT, so it can sit next to Tabuzaur on the profile.

---

Keep this file honest: it is the only project document that is committed. `LOG.md` is
gitignored, so anything that must survive a fresh clone belongs here, not in the log.
