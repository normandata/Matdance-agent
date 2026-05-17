# Local Data Layout

Language: English | [中文](zh-CN/data-layout.md)

Matdance stores important state as local files so users can inspect, back up, and move agent data.

Typical layout:

```text
agents/
  <agent>/
    config/
    sessions/
    memory/
    skills/
    scheduled_tasks/
    workspace/
    runtime/
```

## Config

`config/` stores agent configuration, identity, and user profile. API keys and credentials are sensitive and should not be committed.

## Sessions

`sessions/` stores session metadata and message state:

- conversation messages;
- tool calls and tool results;
- reasoning content if preserved;
- attachments;
- active task state;
- audio attachments and preview markers.

## Memory

`memory/` stores hot memory, core memory, long-term archives, snapshots, and vector files. Long-term memory is dated and more detailed; hot/core are prompt-facing and compact.

## Skills

`skills/` stores reusable skill folders, validation reports, and skill-local assets. Skills can be exported as zip packages.

## Scheduled Tasks

`scheduled_tasks/` stores task definitions, active run state, run files, and execution history.

## Workspace

`workspace/` stores user-visible files produced or used by agents:

- generated images;
- generated audio;
- transcripts;
- uploaded attachments;
- local reports;
- skill or task artifacts.

## Runtime

`runtime/` stores operational state such as browser cookies, runtime events, and local caches. Treat it as sensitive.

## Repository Runtime Root

`.matdance/` stores dependencies, Playwright browsers, Web UI state, auth token state, user time-zone state, entry scripts, and the Web UI shadow runtime.

Do not commit `.matdance/`, `agents/`, browser temp files, logs, API keys, tokens, cookie stores, or generated private workspaces.
