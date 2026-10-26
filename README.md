# Tasketier

**What:** A task tool for Windows that keeps all data on the PC. A global hotkey opens a one-line
box. You type the task with inline marks (`@tomorrow`, `@15:30`, `!`, `#tag`) and press Enter. A
list window shows Today, Upcoming, Inbox and Done. A tray icon keeps the app running.
`tasketier.exe` gives the same store a command line and an MCP server. A Claude Code plugin in
this repo tells an LLM how to use it.

**Why:** Tasks that come to mind during the day get lost, because writing them down takes too
much attention. Capture must take less than two seconds from any window. The daily look-through
must show what is due, what is planned for today and what is done. An LLM must be able to add and
query tasks from a running session.

**Status:** working, 0.1.0. Capture box, list window with the four views, inline edit, plan star,
undo, settings, help, tray, autostart, CLI, MCP server, plugin, installer. 104 tests pass. Every
gate of `scripts\verify.ps1` passes. Not yet checked by hand: the installer's runtime download on
a PC without .NET 10.

## Screenshots

The capture box opens from any window with `Ctrl+Alt+A`. The line below the text shows what the
parser read from it, before you press Enter:

![The capture box with "Call the dentist @fri @15:30 ! #health" typed and the parsed due, priority and tag shown below](docs/img/capture.png)

`?` or `F1` shows every mark:

![The capture box with the inline syntax help unfolded](docs/img/capture-help.png)

The list window, Today view. Overdue tasks come first, in red. Then the tasks due today. Then the
task planned for today, with the star:

![The list window with the Today view: four tasks, one overdue, one with a time and a priority mark, one starred](docs/img/list.png)

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

**Verify:** `.\scripts\verify.ps1` runs four gates. Build with warnings as errors (analyzers, code
style, nullable). Unit tests with coverage. The `dotnet format` check. Cyclomatic complexity
(lizard, limit 9). The script exits non-zero on any FAIL or NOT VERIFIED.

**Prerequisites:** Windows 11 (the installed app runs on Windows 10 1607 or later). .NET SDK
10.0.400 (`net10.0` and `net10.0-windows`). git. lizard (`pip install lizard==1.17.31`) for the
complexity gate. Inno Setup 7 (`C:\Program Files\Inno Setup 7`) for the installer. `nuget.config`
clears the machine-level feeds, so the build is the same on every machine.

## Install

`.\build-installer.ps1` writes `out\installer\Tasketier-Setup-<version>.exe`. The installer:

1. Copies `TasketierApp.exe` (the tray app) and `tasketier.exe` (the CLI and MCP server) into
   `C:\Program Files\Tasketier`.
2. Adds that folder to PATH.
3. Offers a start-with-Windows task.
4. Downloads the .NET Desktop Runtime 10.0 from Microsoft when it is missing. The installer
   checks the SHA-256 of the download.

Tasks and settings live in `%LOCALAPPDATA%\Tasketier`. An uninstall keeps them.

## Use

| Action | How |
| --- | --- |
| Add a task from anywhere | `Ctrl+Alt+A`, type, `Enter`. `Esc` cancels. The focus returns to the previous window. |
| Open the list | `Ctrl+Alt+L`, or a left click on the tray icon |
| Complete | The checkbox, or `Space` on the selected row |
| Plan for today without a date | The star (`☆` becomes `★`). The task stays in Today until it is done or unstarred. |
| Edit | Double-click the title, or `F2`. You edit the whole line in inline syntax. `Enter` saves. |
| Delete | `Del`. `Ctrl+Z` brings the task back. |
| Undo | `Ctrl+Z`. The app keeps the last 20 changes of the session, captures included. |
| Filter | Type in the filter box (`Ctrl+F`). The filter matches the title or a `#tag`. |
| Help | `?` or `F1` in the capture box or the list. Also in the tray menu. |
| Settings | Tray menu: both hotkeys, the data folder, start with Windows |

You can change both hotkeys in Settings. When another program holds a chord, the tray shows a
message, and that hotkey stays off until you change it.

**Inline syntax:** Words are the title. `#tag` adds a tag. `!` is high priority, `!!` is urgent.
Both also work as a suffix: `Pay rent!!`. `@today`, `@tomorrow`, `@mon` to `@sun`, `@2026-11-03`,
`@+3d` and `@+2w` set the due day. A weekday means the next such day, and today counts. `@15:30`
sets the time. Without a day, the time means today. `@none` removes the date. An unknown `@` mark
is an error. The box shows it before you press Enter.

**Views:** Today shows overdue tasks first, in red, then tasks due today, then tasks planned for
today. Upcoming shows tasks due after today, by day. Inbox shows tasks with no date that are not
planned. Done shows tasks completed in the last 30 days.

**Command line:** `tasketier add|list|show|done|undone|edit|delete|plan|unplan|search|mcp|version`.
`--json` prints records for scripts. `--data <folder>` points at a different `tasks.json`.
`tasketier help` shows all commands.

## LLM access (MCP and plugin)

`tasketier mcp` is an MCP server on stdin and stdout. Its tools are `add_task`, `list_tasks`,
`get_task`, `complete_task`, `reopen_task`, `update_task`, `delete_task`, `plan_today` and
`search_tasks`. Each tool returns the changed records as JSON. A failed call returns its message.

The Claude Code plugin in `plugin\` contains a skill and the server registration. The skill says
which tool to use for which request, the inline syntax and the working rules. The registration
starts `${ProgramFiles}\Tasketier\tasketier.exe mcp`. To install the plugin in Claude Code:

```
/plugin marketplace add meet-brad-ch/Tasketier     # or a local clone: /plugin marketplace add C:\Apps\Projects\Tasketier
/plugin install tasketier@tasketier
```

The app, the CLI and the server share one file under a cross-process lock. The app reloads when
the file changes. A task that Claude adds appears in the list at once.

## Storage

The tasks are one JSON file: `%LOCALAPPDATA%\Tasketier\tasks.json`. You can move the folder in
Settings, for example to a synced folder. Each write goes to a temporary file first and then
replaces the real file in one step. The first write of each day copies the previous file to
`backups\tasks-<date>.json`. A damaged file causes an error that names the file. The app never
replaces a damaged file. There is no network access and no telemetry.

## Layout

- `src/Tasketier.Core/` — a library without UI code: task model, inline-syntax parser, JSON store
  (mutex, atomic replace, daily backup, file watcher), view queries, service, undo, settings.
- `src/Tasketier.App/` — WPF (`TasketierApp.exe`): capture window, list window, help window,
  settings window, tray icon, global hotkeys, instance signals, autostart.
- `src/Tasketier.Cli/` — console (`tasketier.exe`): the commands and the `mcp` server
  (`ModelContextProtocol` 2.3.0).
- `plugin/` — the Claude Code plugin. `.claude-plugin/marketplace.json` at the root lists it.
- `tests/Tasketier.Core.Tests/`, `tests/Tasketier.Cli.Tests/` — xUnit. The CLI tests start the
  real exe as an MCP server and drive it with the SDK client.
- `installer/Tasketier.iss`, `build-installer.ps1` — the Inno Setup installer.
- `scripts/verify.ps1` — the quality gates.
- `tools/make_icon.py` — draws the icon. `tools/shoot_windows.ps1` — takes the README screenshots
  from a seeded data folder (see its header).
- `docs/img/` — the README screenshots.

## Not in 0.1

Due-time toasts, recurring tasks and notes per task are candidates for 0.2. Projects, subtasks,
sync, mobile, calendar, attachments, time tracking and export are out.

## Decisions

One line each: the choice, and the reason. Append. Do not rewrite history.

- 2026-10-26 — WPF on .NET 10, not WinUI 3, Avalonia or Python: tray, autostart and settings-file
  code exist from PassKit, and the result is one framework-dependent exe.
- 2026-10-26 — one JSON file, not todo.txt or SQLite: readable by a person, no dependency, some
  hundreds of tasks at most. A cross-process mutex keeps the app, the CLI and the MCP server
  from writing at the same time.
- 2026-10-26 — the 0.1 boundary as listed above. Toasts wait for 0.2, because the first problem
  is capture, not reminding.
- 2026-10-26 — LLM access as a stdio MCP server and a CLI in one exe over the shared store, not
  an HTTP server inside the app. It works when the app does not run, and the WPF app stays free
  of ASP.NET Core. A Claude Code plugin in the repo bundles the skill and the MCP registration.
- 2026-10-26 — default hotkeys `Ctrl+Alt+A` (capture) and `Ctrl+Alt+L` (list). The first choice,
  `Ctrl+Alt+Space` and `Ctrl+Alt+T`, is held by other programs on the owner's PC (measured with
  RegisterHotKey).
- 2026-10-26 — the app is `TasketierApp.exe` and the CLI is `tasketier.exe`. Both live in one
  folder, and Windows file names ignore case, so both cannot be `Tasketier.exe`.
- 2026-10-26 — the installer writes to `C:\Program Files\Tasketier` with elevation, not per user.
  The .NET runtime bootstrap needs elevation, as in PassKit. The plugin's `.mcp.json` uses
  `${ProgramFiles}`.
- 2026-10-26 — public repository under MIT, so it can stand next to Tabuzaur on the profile.
- 2026-10-26 — all text follows Simplified Technical English (ASD-STE100): short sentences, active
  voice, one meaning per word.

---

Keep this file honest. It is the only project document that is committed. `LOG.md` is gitignored,
so anything that must survive a fresh clone belongs here, not in the log.
