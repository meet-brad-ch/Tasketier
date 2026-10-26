---
name: tasketier
description: Manage the user's tasks in Tasketier, the local task tool on this PC, through the tasketier MCP tools (add_task, list_tasks, complete_task, update_task, delete_task, plan_today, search_tasks, get_task, reopen_task). Use when the user asks to add, note, remember or capture a task or todo, asks what is due or planned (today, upcoming, inbox, done), wants a task completed, edited, moved, planned for today, deleted or found, or names Tasketier. Triggers - add a task, remind me to, todo, what is on today, what is due, plan for today, mark done, my tasks, tasketier.
---

# Tasketier

Tasketier keeps the user's tasks in one JSON file on this PC. The `tasketier` MCP server reads and
writes that file. The desktop app shows each change at once. There is no account and no network.

## Which tool

| The user wants | Tool | Notes |
| --- | --- | --- |
| a task written down | `add_task` | one line of inline syntax (below). Returns the task with its `id`. |
| what is on today | `list_tasks` view `today` | overdue first, then due today, then planned |
| what comes later | `list_tasks` view `upcoming` | due after today, by day |
| undated tasks | `list_tasks` view `inbox` | no date and not planned |
| what got done | `list_tasks` view `done` | last 30 days, newest first |
| every open task | `list_tasks` view `all` | overdue first, then by due day, undated last |
| a task done | `complete_task` | needs the `id`. When you only have words, list or search first. |
| a task reopened | `reopen_task` | |
| a task changed | `update_task` | the whole new line. Parts that are not in the line are cleared. |
| a task in today's list without a date | `plan_today` planned `true` | `false` takes it out |
| a task gone | `delete_task` | ask the user first. There is no undo over MCP. |
| a task found | `search_tasks` | title or tag. Open tasks first, done tasks after. |
| one task's details | `get_task` | |

Each tool returns JSON records. A task has `id`, `title`, `tags`, `priority` (None, High, Urgent),
`due` (a date), `dueTime`, `plannedFor`, `created`, `completed` and `isDone`.

## Inline syntax (for add_task and update_task)

Words are the title. Marks can stand anywhere in the line:

- `#tag` - a tag (lower case, several allowed)
- `!` high priority, `!!` urgent. Also as a suffix: `Pay rent!!`
- `@today`, `@tomorrow`, `@mon` to `@sun`, `@2026-11-03`, `@+3d`, `@+2w` - the due day. A weekday
  means the next such day after today. On a Monday, `@mon` is the Monday in one week.
- `@15:30` - the due time. Without a day, it means today.
- `@none` - no date. It wins over every other date mark on the line.

Examples: `Call the dentist @fri @15:30 ! #health`, `Buy milk @tomorrow #home`, `Renew passport @+2w !!`.

An unknown `@` mark is an error, not a title word. Write e-mail addresses without the `@`, or ask
the user how to write the task. A line with no title words is an error.

## Working rules

- Find the id before you change a task: call `search_tasks`, then act on the match. When several
  tasks match, show them and ask which one.
- After a change, repeat the stored title and id: "Added #14 Call the dentist, due Fri 15:30".
- Do not invent due dates. When the user gives no date, add the task without a date. It goes to
  the inbox. When the user says "today", use `@today`. "This week" is not a date. Ask, or leave
  the date out.
- `update_task` replaces every part. To change only the day, send the title, the tags and the
  priority again with the new day. `get_task` gives the current values.
- The server fails with a message when the task file is damaged or locked. Show the message. Do
  not retry in a loop. Never edit `tasks.json` by hand.

## Without the MCP server

The same operations exist on the command line, for a session without the server:

```
tasketier add "Call the dentist @fri @15:30 ! #health"
tasketier list --view today --json
tasketier done 14
tasketier search dentist --json
```

`tasketier help` lists every command. `--json` prints the same records that the tools return.
