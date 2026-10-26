---
name: tasketier
description: Manage the user's tasks in Tasketier, the local task tool on this PC, through the tasketier MCP tools (add_task, list_tasks, complete_task, update_task, delete_task, plan_today, search_tasks, get_task, reopen_task). Use when the user asks to add, note, remember or capture a task or todo, asks what is due or planned (today, upcoming, inbox, done), wants a task completed, edited, moved, planned for today, deleted or found, or mentions Tasketier. Triggers - add a task, remind me to, todo, what is on today, what is due, plan for today, mark done, my tasks, tasketier.
---

# Tasketier

Tasketier keeps the user's tasks in one local JSON file. The `tasketier` MCP server reads and
writes that file; the desktop app shows the changes at once. There is no account and no network.

## Which tool

| The user wants | Tool | Notes |
| --- | --- | --- |
| a task written down | `add_task` | one line of inline syntax (below); returns the task with its `id` |
| what is on today | `list_tasks` view `today` | overdue first, then due today, then planned |
| what comes later | `list_tasks` view `upcoming` | due after today, by day |
| undated tasks | `list_tasks` view `inbox` | no date and not planned |
| what got done | `list_tasks` view `done` | last 30 days, newest first |
| a task done | `complete_task` | needs the `id`; list or search first when you only have words |
| a task reopened | `reopen_task` | |
| a task changed | `update_task` | the whole new line; parts left out are cleared |
| a task into today's list without a date | `plan_today` planned `true` | `false` takes it out |
| a task gone | `delete_task` | confirm with the user first; there is no undo over MCP |
| a task found | `search_tasks` | title or tag, open tasks first, done ones after |
| one task's details | `get_task` | |

Every tool returns JSON records. A task has `id`, `title`, `tags`, `priority` (None, High,
Urgent), `due` (date), `dueTime`, `plannedFor`, `created`, `completed` and `isDone`.

## Inline syntax (for add_task and update_task)

Words are the title. Marks anywhere in the line:

- `#tag` - a tag (lower case, several allowed)
- `!` high priority, `!!` urgent; also as a suffix: `Pay rent!!`
- `@today`, `@tomorrow`, `@mon` … `@sun` (the next such day, today counts), `@2026-11-03`, `@+3d`, `@+2w` - the due day
- `@15:30` - the due time; without a day it means today
- `@none` - no date, wins over other date marks on the line

Examples: `Call the dentist @fri @15:30 ! #health`, `Buy milk @tomorrow #home`, `Renew passport @+2w !!`.

An unknown `@` mark is an error, not a title word: write e-mail addresses without the `@` or ask
the user how to phrase it. A line with no title words is an error.

## Working rules

- Resolve words to an id before changing a task: `search_tasks`, then act on the match. When
  several match, show them and ask which one.
- Quote the stored title and id back after a change: "Added #14 Call the dentist, due Fri 15:30".
- Do not invent due dates. When the user gives none, add the task without a date (it lands in the
  inbox). When they say "today", use `@today`; "this week" is not a date, ask or leave it out.
- `update_task` replaces every part. To change only the day, resend the title, tags and priority
  with the new day; `get_task` gives the current values.
- The server fails with a message when the task file is damaged or locked. Show the message; do
  not retry in a loop and never edit `tasks.json` by hand.

## Without the MCP server

The same operations exist on the command line, when the server is not configured:

```
tasketier add "Call the dentist @fri @15:30 ! #health"
tasketier list --view today --json
tasketier done 14
tasketier search dentist --json
```

`tasketier help` lists every command. `--json` prints the same records the tools return.
