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

## Reliability Boundaries

Every agent and task has concurrency budget limits. Missed work is compensated, not allowed to flood the system without limit.

System-side retry and repair-retry paths exist for failed runs. Old stalled state can be detected through heartbeat and run records.

Agent-visible scheduled task tools intentionally stay narrower:

- `scheduled_task_list` and `scheduled_task_read` inspect task definitions and recent history.
- `scheduled_task_edit` is for user-requested changes such as pausing, resuming, retargeting, or rescheduling.
- `scheduled_task_delete` soft-deletes a task and keeps history.
- `scheduled_task_do` is a manual test run and should only be used when the user asks for it.

The main agent should not perform automatic repair of failed scheduled tasks. If a run fails or stalls, it may explain the visible state, ask the user whether they want a schedule/content change, or use edit/delete when requested. Structural repair, quarantine, retry ordering, and recovery validation remain system responsibilities.
