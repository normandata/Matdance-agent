# What The System Does

Language: English | [中文](zh-CN/system-overview.md)

Matdance is not trying to make a model "sound more human." It gives agents a local operating surface where conversations, state, files, tool results, memory, skills, and maintenance records can survive beyond a single prompt window.

## Conversations Become State

Ordinary chat systems place too much weight on the current context window. When the window is compressed, old details can disappear as if they never existed.

Matdance stores conversation state in local files:

- `sessions/*.json`: session metadata, counts, context usage, activity time, and task state.
- `sessions/*.state.json`: messages, tool calls, reasoning content, sound cue cards, file preview markers, attachments, and active task state.
- `workspace/`: files produced by the agent.
- `runtime/events/` and `scheduled_tasks/runs/`: evidence from background work and recovery.

The chat view is therefore not a thin message relay. It is a visible control surface for local state.

## Memory Becomes Layered Assets

Matdance separates memory by purpose:

- Hot memory: recent working context injected into the prompt.
- Core memory: durable facts and rules injected into the prompt.
- User profile: long-term facts about the user.
- Agent identity: long-term facts about the agent itself.
- Long-term memory: detailed dated archives not normally injected.
- Vector memory: local retrieval index over memory files.

This complements context compression. Compression keeps the current conversation running; memory organization moves durable facts into the right layer.

Long-running users should not keep every project in one endless session. After a phase finishes, or roughly after a week of continuous work, starting a new session gives the organizer a cleaner boundary while preserving important information through cross-session memory.

## Skills Become Verifiable Procedures

Skills are reusable procedures. A good skill explains when to use it, prerequisites, steps, resources, verification, and when not to use it.

Matdance maintains skills through:

- main-agent creation or editing;
- skill organization from completed sessions;
- validation and repair subagents;
- zip export and learn-and-validate import.

External skill packages are treated as untrusted material until localized and validated.

## Background Work Becomes Recoverable

Scheduled tasks write active run state and run files before execution. They update heartbeat while running and record phases, retries, failures, and notices.

After restart, sleep, or interruption, the scheduler compensates missed triggers. Built-in organization tasks are deduplicated by agent/task semantics; skill validation remains idle and state-driven.

## Tools Become Controlled Actions

Tools are not decorations. Browser automation, files, memory writes, skills, scheduled tasks, image generation, and TTS create inspectable results.

Because tools can change state, Matdance treats privacy data, cookies, source code, runtime state, and credentials as separate boundaries. Capability without boundary would eventually turn small model errors into persistent state pollution.

## Web UI Becomes The Console

The Web UI is the main interface because the workflow is larger than CLI chat:

- Chat: messages, tools, tasks, previews, sound cues, browser view, and attachments.
- Agent: model/API/window/budget/avatar configuration.
- Memory: layer editing, long-term browsing, vector search, and map view.
- Skills: editing, organizing, importing, exporting, validating, and repairing.
- Schedule: task definitions, history, catch-up, retry, and repair retry.
- Settings: language, privacy, multimodal profiles, background events, and sound cues.
- Lab: direct multimodal testing.

CLI remains important as a launcher and repair entrance.
