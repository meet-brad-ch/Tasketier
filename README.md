# Tasketier

**What:** A local-only Windows task tool: a global hotkey opens a one-line box, the task is
typed with inline syntax (`@tomorrow`, `!`, `#tag`) and saved on Enter; a list window shows
Today, Upcoming, Inbox and Done; a tray icon keeps it running; `tasketier.exe` exposes the
same store as a CLI and as a stdio MCP server, with a Claude Code plugin bundled in the repo.

**Why:** Tasks that come to mind during the day are lost because writing them down costs
too much attention. Capture has to take under two seconds from any window, and the daily
look-through has to show what is due, what is planned for today and what is done. An LLM
should be able to add and query tasks directly from a running session.

**Status:** scaffold only — README, .gitignore and git exist; no code yet.

## How to run

Nothing runs yet. The intended commands, once the solution exists:

```
dotnet build Tasketier.slnx
dotnet test  Tasketier.slnx
dotnet run   --project src\Tasketier.App                 # tray + hotkeys
dotnet run   --project src\Tasketier.Cli -- add "Buy milk @tomorrow #home"
dotnet run   --project src\Tasketier.Cli -- list --view today
dotnet run   --project src\Tasketier.Cli -- mcp          # stdio MCP server
```

**Verify:** none yet.

**Prerequisites:** Windows 11, .NET SDK 10.0.400 (`net10.0-windows`), git, Inno Setup 7
(`C:\Program Files\Inno Setup 7`) for the installer. A `nuget.config`
with `<clear/>` + nuget.org will be added with the solution (the machine's global NuGet
config holds an unreachable feed).

## Layout (planned)

- `src/Tasketier.Core/` — engine-free library: task model, inline-syntax parser, JSON store
  (cross-process mutex, atomic replace, daily backup), view queries, planning rules, undo.
- `src/Tasketier.App/` — WPF: capture window, list window, tray icon, settings, global hotkeys.
- `src/Tasketier.Cli/` — console exe `tasketier.exe`: CLI subcommands and `mcp` (stdio MCP server).
- `plugin/` — Claude Code plugin: `skills/tasketier/SKILL.md` and `.mcp.json` registering the server.
- `tests/Tasketier.Core.Tests/`, `tests/Tasketier.Cli.Tests/` — xUnit.
- `scripts/` — `verify.ps1` once sw-quality sets it up; `build-installer.ps1` for the Inno Setup installer.
- `installer/Tasketier.iss` — Inno Setup script (modelled on PassKit.iss, bootstraps the .NET Desktop Runtime).

## MVP

1. **Quick capture:** `Ctrl+Alt+Space` opens a borderless box on the active monitor; Enter
   saves, Esc cancels, focus returns to the previous window. Inline syntax: `!`/`!!`
   priority, `@today` `@tomorrow` `@fri` `@2026-11-03` `@15:30` due, `#tag`. The box
   previews what it parsed.
2. **List:** `Ctrl+Alt+T` or tray click. Views Today (overdue on top, due today, planned for
   today), Upcoming (by day), Inbox (no date), Done (30 days). Checkbox completes, inline
   edit, Del with undo, type-to-filter, "plan for today" star with carry-over.
3. **Tray:** open-today count in the tooltip; left click list; right click Add / Open /
   Settings / Exit; starts with Windows.
4. **Storage:** one JSON file in `%LOCALAPPDATA%\Tasketier` (folder changeable), atomic
   write, daily backup. No network, no telemetry.
5. **Settings:** hotkeys, data folder, autostart, theme follows Windows.
6. **Safety:** undo for complete and delete; a corrupt file is reported, never replaced silently.
7. **LLM interface:** `tasketier.exe add|list|done|undone|edit|delete|plan|unplan|search`
   (`--json` for scripts) and `tasketier.exe mcp` with tools `add_task`, `list_tasks`,
   `complete_task`, `update_task`, `delete_task`, `plan_today`, `search_tasks`. The app
   reloads on file change, so tasks added by an LLM appear at once.

Not in the MVP: due-time toasts, recurring tasks, notes per task (all v0.2); projects,
subtasks, sync, mobile, calendar, attachments, time tracking, export.

## Decisions

One line each: the choice, and the reason it was made. Append; do not rewrite history.

- 2026-10-26 — WPF on .NET 10 over WinUI 3 / Avalonia / Python: the owner's PassKit already
  has working tray, autostart, foreground-tracking and settings-file code to reuse.
- 2026-10-26 — one JSON file over todo.txt or SQLite: human-readable, no dependency, hundreds
  of tasks at most; a cross-process mutex keeps app, CLI and MCP server from racing.
- 2026-10-26 — MVP boundary as listed above; toasts wait for v0.2 because forgetting is a
  capture problem first.
- 2026-10-26 — default hotkeys `Ctrl+Alt+Space` (capture) and `Ctrl+Alt+T` (list), configurable.
- 2026-10-26 — LLM access as stdio MCP + CLI in one exe over the shared store, not an HTTP
  server inside the app: works when the app is not running, no ASP.NET Core in a WPF app;
  a Claude Code plugin in the repo bundles the skill and the MCP registration.
- 2026-10-26 — distributed as an Inno Setup installer (PassKit pattern) into
  `%LOCALAPPDATA%\Programs\Tasketier`; that fixed path is what the plugin's `.mcp.json` points at.
- 2026-10-26 — source lives in a private GitHub repo under `meet-brad-ch`
  (`https://github.com/meet-brad-ch/Tasketier`); the owner pushes.

---

Keep this file honest: it is the only project document that is committed. `LOG.md` is
gitignored, so anything that must survive a fresh clone belongs here, not in the log.
