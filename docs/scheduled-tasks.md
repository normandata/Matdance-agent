# Scheduled Tasks

Language: English | [中文](zh-CN/scheduled-tasks.md)

Matdance scheduled tasks are persistent local work items. They are designed to survive restarts, sleep, missed triggers, and partial failures.

## Schedule Types

Supported patterns include:

- one-time tasks;
- interval tasks;
- daily tasks;
- daily-count tasks;
- daily-window tasks;
- daily-times tasks.

Tasks are stored under each agent's `scheduled_tasks/` directory.

## Run State

Before a run starts, Matdance writes active run state and a run record. During execution it updates heartbeat and records phases, retries, tool results, failures, and final notices.

This is not a database transaction, but it prevents many ghost locks and unreadable partial files.

## Catch-Up

After restart, sleep, or interruption, the scheduler can compensate missed triggers.

User-created tasks catch up according to their scheduled times. Built-in organization tasks are deduplicated by `agent + taskId`, because one organization run can cover all missed organization windows.

Skill validation is not a startup catch-up task. It is driven by skill state and idle budget.

## Built-In Tasks

Matdance registers built-in memory organization and skill organization tasks for each agent. Their displayed title can be localized in the UI, but their internal registration text stays stable.

## Delivery Targets

Scheduled-task results can be delivered to:

- `created_session`: the normal chat session where the task was created.
- `session`: one user-confirmed normal chat session id returned by `session_list`.
- `all_agent_sessions`: all normal sessions for the current agent only.
- `notification_session`: a dedicated read-only notification session. Its user-facing session name is the task title, while routing still uses a unique internal session id.

Dedicated notification sessions are visible in the session list and persist like other sessions, but the chat composer is disabled there. They only accept scheduled-task result delivery. Memory and skill organization ignore these sessions for now, and all-sessions delivery skips them.

Delivered scheduled-task notices render the full subagent Markdown output in the notification card. The UI only strips Matdance's fixed low-priority footer; separators, tables, headings, and other content inside the subagent result are preserved.

## Reliability Boundaries

Every agent and task has concurrency budget limits. Missed work is compensated, not allowed to flood the system without limit.

System-side retry and repair-retry paths exist for failed runs. Old stalled state can be detected through heartbeat and run records.

Agent-visible scheduled task tools intentionally stay narrower:

- `session_list` lists current-agent session ids, display titles, kind/read-only flags, and short metadata so the user can confirm a normal target session for delivery.
- `scheduled_task_create` creates a task only after the timezone, schedule, content, and delivery target are clear. The default delivery target is the current normal session where the task is created. "All sessions" means all normal sessions of the same agent only. A specific old session target requires a user-confirmed `sessionId`. A new/dedicated notification session uses `notification_session` instead.
- `scheduled_task_list` and `scheduled_task_read` inspect task definitions and recent history.
- `scheduled_task_edit` is for user-requested changes such as pausing, resuming, retargeting, or rescheduling.
- `scheduled_task_delete` soft-deletes a task and keeps history.
- `scheduled_task_do_a_test` queues a manual test run for user-created tasks and should only be used when the user asks for it. Main-agent tool calls do not run the test synchronously; they place it into the highest-priority scheduled-task queue so it can start after the current conversation releases concurrency budget. The test does real work and delivers the result to the configured targets by default, but it does not advance `nextRunAt`, `runCount`, failure count, backoff, or the last scheduled-run fields. System built-in tasks cannot be manually tested.

The main agent should not perform automatic repair of failed scheduled tasks. If a run fails or stalls, it may explain the visible state, ask the user whether they want a schedule/content change, or use edit/delete when requested. Structural repair, quarantine, retry ordering, and recovery validation remain system responsibilities.
