# Matdance Full Documentation

Language: English | [中文](FULL-DOC.zh-CN.md)

Current version: v1.1.18-preview

This file is the complete explanatory document for Matdance. `README.md`, `quickly_start.md`, and the topic documents under `docs/` stay concise so that entry points, commands, and local mechanisms are easy to find. If you want to understand what this system is doing, why it is shaped this way, and which boundaries must not be blurred, read this `FULL-DOC.md`. `FULL-DOC.md` and `FULL-DOC.zh-CN.md` should carry the same complete content; they differ only by language.

Matdance is not a forwarding shell with a chat window wrapped around it. It is a local C# agent runtime. It puts the Web UI, sessions, memory, skills, workspaces, scheduled tasks, browser automation, file previews, multimodal asset generation, and background maintenance tasks into one local persistent system.

The problem it tries to solve is direct: let agents keep local state, organize experience, reuse skills, and leave files that a human can inspect at any time. "Continuous learning" here does not mean secretly training model weights, and it is not an agent roleplaying growth inside a chat box. Matdance accumulates Markdown, JSON, indexes, task records, run reports, and validation reports. Only something visible, editable, and movable deserves to be called long-term collaboration.

## v1.1.18-preview Summary

v1.1.18-preview continues the boundary and reliability work that started in v1.1.17.

- README was split into a concise entrance, topic documents, and this full document. README no longer carries every explanation.
- Built-in memory organization and skill organization tasks now use stable English registration text, while the Web UI displays their titles and descriptions in the current UI language.
- Background events and subtask stage text use stable English runtime data, avoiding mixed Chinese status text in the English UI.
- Skill validation reports with `needs_changes` or `invalid` status continue to enter the pending validation queue, so skills can keep improving through validation and repair loops.
- Skills can be exported as zip packages; importing still goes through learning and validation, so external skill packages do not bypass local safety checks.
- Catch-up for built-in memory organization and skill organization is deduplicated by `agent + taskId`. If several triggers were missed while offline, each organizer is compensated once. Skill validation is not part of startup catch-up and remains driven by idle state.
- Tasks created through `task_manager` keep at most 3 steps so long checklists do not overcrowd the Chat UI.
- Long-term memory detail preview uses an internal scroll panel, so large archive files no longer stretch the whole Memory page.
- Privacy Access liveness is written into the system prompt and tool descriptions. The current Settings state is the only authority for that permission signal.
- Browser automation is background-first: it does not raise native foreground windows, `browser_close` is a no-op for normal agent calls, and the system maintains a shared browser/context/page hot state.
- `browser_evaluate`, navigation, screenshots, content reading, title reading, and the browser global operation lock all have timeout boundaries so one tool call cannot block the queue indefinitely.
- Cookie tools remain available but never return raw values. `apply_cookie` returns diagnostics that explain that writing cookies into the context does not mean the current page is immediately logged in.
- Image attachments have provider/model-level vision capability caching. Unknown models get one chance to receive image payloads; after explicit rejection or repeated "image failed, text-only succeeded" cases, later turns default to text-only for that provider/model.
- Long TTS text can be split by sentence into at most 10 chunks, synthesized in parallel, and merged into final audio. Playback failures should be surfaced through a UI error layer instead of being shoved under a chat bubble.

## Current Capabilities

- Local multi-agent management: every agent has independent configuration, identity, user profile, sessions, memory, skills, scheduled tasks, and workspace.
- Web UI first: daily chat, configuration, memory, skills, scheduled tasks, vector memory atlas, and Lab debugging all happen through the Web UI.
- Main wrapper menu: supports dependency installation, hosted Web UI, supervisor mode changes, status checks, stopping Web UI/hook/keep/boot, and system-level `matdance` entry registration.
- Streaming chat and tool calls: model replies, tool results, task steps, file previews, and sound cue states are written back into the session.
- Thinking-model compatibility: the system can parse multiple reasoning fields. Thinking output is temporarily disabled on OpenAI-compatible and Anthropic-compatible request paths for stability, avoiding repeated reasoning text or malformed tool requests in reasoning segments.
- Cross-session memory: hot memory, core memory, long-term memory, and vector memory divide responsibility.
- Local vector database: no cloud embedding calls. It uses local feature hashing, SimHash, VP-tree, and rerank for fuzzy retrieval, and the Memory page exposes vector search plus a 2D atlas.
- Skill system: supports manual editing, organization, zip export, learning and validating external material, manual validation, idle auto-validation, and limited automatic repair.
- Scheduled task system: built-in memory organization every 3 hours and skill organization every 3 hours; after server restart, sleep, or interruption, missed triggers are compensated. User tasks compensate per missed slot, while system organizers fold catch-up by task semantics.
- Browser automation: controlled Playwright Chromium with navigation, clicking, typing, screenshots, page reading, short JavaScript, and cookie save/list/apply.
- File attachments: Chat supports up to 3 attachments, including common images, documents, and zip files. Images are first attempted as model multimodal payloads.
- Inline file previews: chat messages can use `{show_file:...}` to display HTML, images, Markdown, code, text, audio, and some browser-openable documents.
- Multimodal endpoints: built-in `image_generation` and `text_to_speech` asset tools, plus Chat/Lab TTS and STT components.
- Local-first state: most agent data is under `agents/`, where files can be opened and inspected directly without relying on a cloud black box.

## Startup And Configuration

Startup, dependency installation, system-level `matdance` entry registration, agent model configuration, and multimodal endpoint configuration are covered in [quickly_start.md](quickly_start.md).

Commands are not repeated here for a simple reason: the startup document should get the system running; the full document should explain why the system works this way. Packing commands, parameters, disclaimers, design philosophy, and every subsystem detail into one full document would be expressive, but it would be harder to search.

The recommended path is:

- Install the .NET 9 SDK.
- Install Playwright Chromium dependencies before using browser automation for the first time.
- Start the Web UI through the source wrapper script or through the registered `matdance` entry.
- Use the Web UI for daily chat, configuration, memory, skills, scheduled tasks, and Lab debugging.

Matdance has no Java or npm project dependency. Playwright brings its own Node runtime for installing and driving the browser. That is not a requirement for the user to install Node/npm, and it is not a Java dependency.

## Web UI

The Web UI is the current recommended primary entry point for Matdance. It carries the complete daily workflow: chat, configuration, memory, skills, scheduled tasks, vector memory search and atlas, and Lab debugging. The CLI is a launcher, repair entrance, and low-level command set, not the full daily interaction surface.

By default, the Web UI should bind only to local loopback addresses. Non-loopback hosts are rejected unless `MATDANCE_ALLOW_REMOTE_WEB=1` is explicitly set. Remote binding enables single-token authentication: only one token is valid at a time, browser login stores auth state through an HttpOnly cookie, and system APIs also accept `Authorization: Bearer <token>` or `X-Matdance-Token`.

Main Web UI areas:

- Home: the planet entry page for moving between pages.
- Chat: the main agent conversation page with streaming messages, tool call display, task progress, inline file preview, browser view overlay, attachments, voice input, and reply audio playback.
- Agent: configuration page for creating/deleting agents and changing model, base URL, API type, context window, concurrency budget, avatar, and related settings.
- Schedule: scheduled task management page for once, daily, repeated, and window-loop tasks, plus run history.
- Skills: skill library page for creating, editing, deleting, exporting, organizing, validating, and learning external skill material.
- Memory: memory management page for core/hot/user/identity, long-term memory browsing, local vector search, and atlas view.
- Lab: debugging page for image generation, speech synthesis, and speech-to-text, used to expose configuration illusions.
- Settings: language, local preferences, Privacy Access, memory limits, background events, sound cues, and multimodal endpoint configuration.

## Local Data Layout

`agents/` is the root directory for all agent-preserved data. Most state for an agent can be located by its name.

Typical structure:

```text
agents/
  multimodal_config.json
  <agent-name>/
    config/
      agent_config.json
      identity.md
      user.md
    icons/
    sessions/
      <session-id>.json
      <session-id>.state.json
    memory/
      hot_memory/
        MEMORY.md
      core_memory/
        core_memory.md
      long_term_memory/
        YYYY-MM-DD.md
      vector_memory/
        base.json
    skills/
      <skill-id>/
        skill.md
        skill.json
        validation-report.md
        import-report.md
        scripts/
        assets/
        templates/
        resources/
        examples/
        config/
    scheduled_tasks/
      tasks.json
      runs/
        <task-id>/
          <run-id>.json
    runtime/
      browser_cookies/
        cookies.json
      events/
      jobs/
    workspace/
      attachments/
      generated/
        images/
        audio/
```

`agent_config.json` may contain API keys. `runtime/browser_cookies/cookies.json` may contain reusable login state. `sessions/`, `workspace/`, `scheduled_tasks/runs/`, `runtime/events/`, and `runtime/jobs/` are local runtime traces and should not normally be committed.

`.matdance/` is the local machine runtime directory, not a source asset. It stores dependencies, Playwright browsers, Playwright driver cache, Web UI state, Web auth tokens, user time zone state, registered entry scripts, Web UI shadow runtime, and model capability cache.

Typical structure:

```text
.matdance/
  deps/
    playwright-browsers/
    playwright-driver/
  state/
    web-ui.json
    web-auth.json
    user-time-zone.json
    model-capabilities.json
  web-ui-shadow/
  bin/
  logs/
```

Important JSON writes go through a same-directory temporary file before replacing the target file. This is not a database transaction, but it reduces truncated JSON, ghost run locks, and post-crash unreadable state.

## Sessions

`sessions/` stores session data. Each `<session-id>.json` stores session statistics; `<session-id>.state.json` stores messages, tool calls, current tasks, and UI state.

Common fields:

- `session_id`: session ID.
- `context_usage`: current prompt tokens relative to the agent's `context_window`.
- `total_messages`: total user messages originating from this session.
- `tool_messages_count`: number of tool-result messages.
- `tokens`: locally estimated total context tokens.
- `create_at`: session creation time.
- `last_activity`: most recent activity time, used to judge whether an agent is idle.
- `is_processing`: whether this session is processing the main agent turn.
- `tasks[]`: tasks extracted inside the session.

Session timestamps are written in the Matdance user time zone with an offset. The Web UI updates `.matdance/state/user-time-zone.json` from the browser-reported time zone; `MATDANCE_TIME_ZONE` can also pin it. Session IDs, run IDs, scheduling deduplication, cookie expiry, token expiry, and process timeouts still use UTC/Unix time.

## Memory

Memory is cross-session state, one of the biggest differences between Matdance and a normal chat shell. It is not a rough pile of complete chat logs; it separates information into layers with different stability.

### Hot Memory

Path:

```text
agents/<agent>/memory/hot_memory/MEMORY.md
```

Hot memory is recent important context and is injected into the prompt. It is suitable for things that happened recently and still matter in the short term: current task progress, newly formed agreements, recent issue status, and clues that will still be needed in the next few days.

The key point is that hot memory is treated as a recent working set, not a long-term archive. Normal in-chat `memory_store` remains append-only so the main agent does not accidentally delete old content during a single turn. Background memory organization tasks, however, fully read the hot/core/user/identity/long-term files in their responsibility scope and return complete replacement payloads. Organizers are allowed to move old, already archived, low-immediacy hot memory out of the working set instead of keeping it permanently inside the prompt.

Space management is therefore not just "compress old records thinner and thinner." Recent context with operational value has higher priority: current tasks, unresolved issues, newly formed agreements, short-term commitments, and clues needed soon should not be compressed into useless one-line residue. When capacity is tight, the system first removes older content that has already entered long-term memory, or keeps only a date-index pointer.

There is no brute-force truncation here. Before writing, if the complete hot/core/user/identity payload exceeds the corresponding limit, the host refuses the write, preserves the original files and bookmark state, and returns status describing which field exceeded the limit, the estimated token count, and the limit. The real dilution, discarding, and archiving still belongs to the organizing subagent, not to the host cutting a string in the middle.

Memory organization also has an adaptive downgrade controller. By default, it fully reads the hot/core/user/identity/long-term files in its scope and processes new messages and task run records in small batches. If the LLM returns context, payload, or token-related errors, the system first reduces the new input batch size for this turn. If the full memory base context itself is still too heavy, it enters layer-by-layer organization, updating only one target layer or a group of layers among `user_md`, `identity_md`, `core_memory`, `hot_memory`, and `daily_memories`, while non-target layers are provided only as boundary summaries. After each layer succeeds, the system retries merging remaining layers back into a more complete mode. If a layer still fails, it downgrades the input batch for that layer; long-term memory can also fall back to injecting date-specific evidence files.

Layer recovery does not write files recklessly. Before entering the layered chain, the system creates a memory snapshot. Inside the chain, layers can be written one by one because later layers need to see the weight reduction already completed. If the batch ultimately still fails, the system rolls back to the snapshot from the start of that batch, avoiding half-finished memory contamination.

The point of this mechanism is "fail downward, recover upward." Successful batch sizes are written into the agent's global bookmark state. Next time, the organizer starts between the known working value and the default value, then gradually returns to the default after consecutive success. This does not permanently shrink the system; it lets organization continue when model context ability is insufficient, history has swollen, or one memory layer is too heavy, while preserving layer responsibility.

Usage should cooperate with this mechanism. Matdance has cross-session memory; you do not need to keep every conversation in one endless session. After a phase of work is complete, or roughly after a week, starting a new session is usually better. Important information continues through hot/core/long-term/vector memory, so a new session does not make the agent forget. Instead, it gives incremental organization a cleaner boundary and reduces pressure from overly long sessions, too many tool records, or heavy attachments.

If an old session becomes a long-term organization debt, a new session can still serve as a fresh low-entropy input stream. That is a win on both sides: the user keeps long-term continuity, the system lowers organization cost, and memory classification becomes more accurate.

Hot memory may mention things that did not happen today, but it should stay index-like and summary-like. It should not become the full text of a long-term archive. Its value is helping the system reconnect to current state after context compression and keeping memory consistent across sessions.

### Core Memory

Path:

```text
agents/<agent>/memory/core_memory/core_memory.md
```

Core memory contains more stable and central facts and is injected into the prompt. It should distinguish:

- Long-term user preferences: name, personality, communication style, likes, dislikes, long-term goals, and stable traits.
- Long-term agent preferences: what the agent is called, what service style it tends to use, how it prefers to express itself, and what collaboration pattern fits it.

When writing manually, do not put daily trivia into core. Only truly long-lived information belongs in core memory.

Strictly speaking, memory usually also has `user.md` and `identity.md` as two long-term preference Markdown documents. Core memory can also record long-term preferences, but it is not the primary carrier. Long-term preferences are mainly maintained in `user.md` and `identity.md`; these files are also injected into the prompt. This handles long time scales where core memory may become too crowded and need dilution. The cost is some extra tokens, and the tradeoff is usually worth it.

### Long-Term Memory

Path:

```text
agents/<agent>/memory/long_term_memory/YYYY-MM-DD.md
```

Long-term memory is saved by date and is not injected into the prompt as a whole by default. It is suitable for questions like "what did we do that day", "what did we learn last week", or "what was the state of that project then".

Long-term memory should be more detailed than hot memory. It is more like a document in an archive than a short note in the prompt. Hot memory can act as an index, while long-term memory preserves the detailed facts collapsed from that day.

Hot memory records content by event date. That means an agent can use hot memory as an index to query the long-term memory file for that day when answering user questions like "what happened that day". It can also compare the user's local time to determine precise dates for questions like "what was yesterday", then index by that exact date.

### Vector Memory

Path:

```text
agents/<agent>/memory/vector_memory/base.json
```

Vector memory is a local retrieval index, not a knowledge source. The sources of knowledge remain the hot/core/long-term Markdown files.

The current algorithm is `matdance-local-hash-v1`:

- embed: feature hashing over English/numeric words, Chinese characters, Chinese bigrams/trigrams, and English word trigrams to produce fixed sparse vectors.
- index: SimHash Hamming distance is used to build a VP-tree.
- search: queries first use the VP-tree for approximate candidate recall.
- rerank: sparse cosine, term overlap, and Hamming similarity are combined, with a small boost for hot/core sources.
- atlas: the Web UI projects indexed snippets into 2D nodes to show approximate relationships between memory fragments.

Its strengths are being local, rebuildable, explainable, and cheap. Its boundary is that it is not real semantic understanding and does not promise exact top-k results. Treating it as a "local brain" is wrong. Treating it as an inspectable, rebuildable, low-cost memory index is the right posture.

It is suitable for questions like "do you still remember the key engineering problem we met in that project" or "do you remember why I cried last time", where there may be no precise date source and the content may be fuzzy or hallucination-prone. Such content may have been captured by the memory documents above, but over long time scales every category can still be diluted. When that happens, vector memory is the best fallback and guardrail. The algorithm may look and feel less reliable than mature systems, but a design that can stand up at the critical moment is still valuable. We do not need to overstate it.

### Memory Organization Boundaries

Memory organization subagents must output complete new content. They should not write placeholders such as "unchanged", "omitted", or "see above", because Matdance writes the organization result back to files and placeholder text would overwrite old content and cause information loss.

Memory may preserve wishlists, guesses, commitments, future plans, or ordinary chat summaries, but it must classify them clearly. It must not rewrite an unrealized commitment as a completed fact. `user.md` records long-term user preferences, `identity.md` records the agent's own long-term service style and identity preferences, hot memory records recent state, and long-term memory keeps date archives.

## Skills

Skills are stored under:

```text
agents/<agent>/skills/<skill-id>/
```

A skill package usually contains:

- `skill.md`: the skill manual with frontmatter, the main file read by both humans and agents.
- `skill.json`: skill metadata.
- `validation-report.md`: validation report with content fingerprint and maintenance mode.
- `import-report.md`: localization report left after learning and validating external material.
- `scripts/`, `assets/`, `templates/`, `resources/`, `examples/`, `config/`: real resources required by the skill.

A skill is a reusable operating manual that an agent writes for its future self. It is not a normal chat summary and not a wishlist. Only methods that have been practiced, have clear results, and can be reused should become skills.

A skill should include:

- When to use it.
- Concrete steps and decision conditions.
- Practiced commands, paths, APIs, or file structures.
- Known limits and failure handling.
- Required local resource paths.

A skill should not include:

- Unpracticed promises.
- Uncertain guesses.
- Ordinary chat summaries.
- Private data handling templates.
- Credentials, tokens, raw cookie values, or account recovery procedures.
- References to nonexistent resources.
- Paths that only worked in one temporary directory.

If a skill manual mentions a script, program, template, configuration, or other local resource, that resource must live inside the skill directory, and the manual must point to it with a clear path such as `./scripts/example.py`, `./templates/report.md`, or `./assets/schema.json`.

"Learn and validate" does not mean copying an external skill into the system as-is. External text, README files, zip files, folders, and scripts should all be treated as untrusted material. The learning subagent's job is to localize the safe, reusable parts that can fit into Matdance's directory structure. Rewritten paths, unsupported assumptions, safety issues, and skipped files are recorded in `import-report.md`.

Skills can be exported as zip files. Export packs the existing files under the skill directory as-is: `skill.md`, `skill.json`, reports, scripts, templates, resources, and examples. Whatever is present is included. Skills therefore become more than single-machine folders; they can become shareable ecosystem assets.

Import should still go through "learn and validate". Even if a zip came from another Matdance instance, it should be treated as external material rather than directly overwriting a local skill directory. Learning and validation re-check content, rewrite unsuitable paths, skip risky files, generate `import-report.md`, and then enter validation and repair. Export makes skills portable; learning and validation prevent portability from becoming uncontrolled feeding.

After v1.1.18-preview, skills with validation report status `needs_changes` or `invalid` continue to enter the pending validation queue. This keeps a skill from being stranded in a half-broken state after one failed report; when the system has idle budget, it can keep validating, repairing, and validating again, aiming for a self-consistent loop. There is a premise: when you write skills manually or ask an agent to write them, do not put impossible or unprovable requirements into the skill manual, such as asking it to solve a century-level mathematical problem as a metaphor. Some things are not solvable by an agent; writing them into a skill is no better than an attack that wastes time and tokens. If the skill was created by the system organizer from practiced work, this risk is much lower.

## Scheduled Tasks

Matdance's scheduled task system lets agents keep doing low-priority maintenance when the user is not staring at the chat box. It is not a strict real-time task queue and not an OS-level workflow engine. It is closer to a local recoverable background debt system.

Scheduled tasks are stored in:

```text
agents/<agent>/scheduled_tasks/tasks.json
agents/<agent>/scheduled_tasks/runs/<task-id>/<run-id>.json
```

Schedule types:

- `once`: run once.
- `daily`: run at a fixed time every day.
- `daily_times`: run at multiple fixed times every day.
- `daily_window`: trigger repeatedly inside a daily time window.
- `interval`: interval trigger used by system tasks; built-in organizers default to every 180 minutes.

Each agent automatically registers two system tasks:

- `sched_system_memory_org`: System Memory Organization. Organizes hot/core/user/identity/long-term memory and refreshes related indexes.
- `sched_system_skill_org`: System Skill Organization. Analyzes recent sessions, extracts or updates reusable skills, and writes them into the skill library.

System tasks cannot be changed by normal edit or delete operations. Their registration text uses stable English data, and the Web UI displays titles and descriptions according to the current language. This prevents task files from depending on one UI language choice.

After Web UI restart, computer sleep, or service interruption, the background worker or system-level hook compensates missed triggers, but compensation is based on task semantics.

User-created scheduled tasks still compensate missed slots by `scheduledAt`, and run records use `scheduledAt` to avoid duplicate execution. These tasks may truly require one result for every missed trigger.

Built-in memory organization and skill organization are different. They are incremental organization tasks; one organization run can cover the accumulated changes from downtime. Therefore catch-up folds by `agent + taskId`: one agent's memory organizer is compensated at most once, and one agent's skill organizer is compensated at most once, advancing the cursor to the latest missed trigger. This preserves recovery ability while avoiding several equivalent organization runs after one night offline.

Skill validation is not part of startup compensation. It is state debt, not time debt: only when a skill is currently unvalidated, invalidated, or reported as `needs_changes` or `invalid`, and when the agent is idle, does the idle validation worker validate skills one by one by `agent + skillId`. That trigger point is itself the stability boundary and should not be reordered by server startup compensation.

To avoid a large burst after long downtime, each normal task compensates at most 8 missed trigger points in one round, and each agent takes at most 25 due items per round. The system-level hook foreground compensation is also limited to at most 25 tasks per run.

Every agent has its own `max_concurrency`. User messages, Web UI manual organization/validation/execution, background scheduled tasks, memory organization, skill organization, and skill validation all consume that budget. If you do not understand the concurrency limits of your model provider, keeping the default `1` is usually more stable.

Shared resources also have additional locks, such as memory files, skill files, and scheduled task files. If a resource lock cannot be acquired, the scheduler first tries other runnable tasks. If nothing else can run in this round, it waits for the resource lock and re-sorts in the next round.

After a task gets budget and resource locks, it immediately persists an active run and writes a `running` run record. During execution, model requests, model retries, tool calls, subtask stage changes, and notification delivery all refresh the heartbeat. Only after the task has truly started and then has no heartbeat for 10 consecutive minutes is it marked `stalled`, enters a 30-minute backoff, and is moved to the lowest recovery priority.

The Schedule page provides two actions for abnormal tasks:

- Retry: clear backoff and enqueue immediately.
- Repair and retry: clone and normalize the task structure, mark the old active item as `replaced`, keep the same task ID and run history, then enqueue again.

These recovery actions are UI/system-side controls, not broad agent repair powers. Agent-visible scheduled task tools stay limited to listing, reading, user-requested edit/reschedule/pause/resume, explicit test runs, and soft deletion. If a scheduled task fails or stalls, the main agent should explain visible state and ask whether the user wants a concrete edit or deletion; structural repair, retry ordering, quarantine, and recovery validation remain system responsibilities.

The background events panel in Settings -> General displays recent subagent, scheduler, and recovery events by agent, and summarizes completed, incomplete, skipped, failed, and remaining items. Background events are not decorative lists; they are the entry point for diagnosing the background system.

## Tools

### Bash

The `bash` tool runs commands under the agent workspace and has timeout limits. On Windows, it is actually routed through the shell configured by the current runtime; on macOS/Linux, it uses the corresponding shell. The system prompt injects the current OS, architecture, shell, and path style so the model can choose correct commands.

Web UI mode does not pop up interactive confirmation windows. Dangerous commands are rejected or pushed outside the CLI/manual maintenance boundary. Long-running foreground services, watchers, and daemons should not be put into this tool and left occupying the window.

### File Tools

`file_read` and `file_write` are mainly for the agent workspace and preview-safe runtime outputs. Matdance source code, plugin source code, `.matdance/state`, run queues, task run records, cookie store, agent config, credentials, and authorization files should not be accessed or modified through agent file tools.

When a file is valuable to the user, the agent should display it in the visible reply with `{show_file:PATH}`.

### File Preview

When a chat message contains `{show_file:path}`, the Web UI replaces that marker with an inline preview card. It is not a tool call; it is a frontend rendering rule.

Path resolution goes through `/api/file`. With an agent parameter, ordinary relative paths are resolved from the current agent's `workspace/` first. Without an agent, the system tries each agent's `workspace/`, the project root `browser_temp/`, and read-only built-in sound cue resource directories. Absolute paths can be used only if they stay within preview-safe roots. Sensitive files such as `config/`, `multimodal_config.json`, cookie stores, and runtime state are not exposed through the preview endpoint.

Supported types:

- Images: displayed inline.
- HTML: rendered in an iframe.
- Markdown: fetched and rendered.
- Text/code: displayed as text.
- Audio: inline player.
- PDF/Office documents: an open entry is provided, with exact behavior depending on the browser.

### Browser Automation

Matdance includes controlled Chromium based on Playwright. The browser service is a global singleton and is not isolated by agent or session. It currently shares one browser/context/page, and operations are serialized by locks. The Browser overlay in the Web UI can observe the live page.

Current browser policy:

- Background-first. `headless:false` requests are ignored; the system does not bring a native browser window to the foreground and disturb the user.
- Preserve hot state. Refreshing, switching pages, reopening the browser, or `browser_close` should not be treated as universal recovery tools.
- `browser_close` is a compatible no-op under normal agent calls. The browser is cleaned up when the Web UI closes or the host releases it.
- `browser_evaluate` is only for short JavaScript: synchronous DOM reads, light clicks/assignments, and fast status checks.
- Navigation, screenshots, content reads, title reads, and the global operation lock all have timeout boundaries to avoid long-term queue occupation.
- Login, verification codes, CAPTCHA, account selection, and similar steps should be handled by the user through an available user-controllable authentication surface.

Common tools:

- `browser_navigate`: navigate to a specified URL.
- `browser_click`: click a page element by CSS selector.
- `browser_type`: type text into an input by CSS selector.
- `browser_screenshot`: capture the current page, usually saving to `browser_temp/`.
- `browser_get_content`: get current page text or HTML.
- `browser_evaluate`: execute short JavaScript and return the result.
- `browser_wait_for`: wait for dynamic readiness through selector, text, URL, load state, or a short safe predicate.
- `browser_query`: inspect structured DOM candidates and selector hints before interacting.
- `browser_scroll`: perform bounded lazy-load scroll steps, optionally stopping on selector/text.
- `browser_inject_init_script`: add a small future-navigation init script; cookie/storage/credential/token, CAPTCHA/paywall, network interception, and anti-bot bypass patterns are rejected.
- `save_cookie`: save cookies from the current browser context.
- `list_cookie_by_site`: list saved cookie coverage by site without returning values.
- `apply_cookie`: apply saved cookies back into the current browser context.
- `browser_close`: no-op under normal agent calls.

Successful `apply_cookie` only means cookies were written into the controlled browser context. It does not mean the current page immediately becomes logged in. Already loaded pages may need normal site navigation or reload before they read new cookies. If the current page is outside the target site scope, the browser must navigate to the target site first. When facing a login wall, the correct behavior is to have the user complete login, then save cookies, rather than repeatedly closing, refreshing, or bypassing the login interface.

The dynamic helpers are for ordinary client-side rendering, delayed content, and lazy-loaded result lists. They are not an authorization, CAPTCHA, paywall, anti-bot, or terms-of-service bypass layer.

### Multimodal Tools

`image_generation` generates images through the OpenAI-compatible `/images/generations` endpoint configured in Settings and saves results under the current agent's `workspace/generated/images/`. When `profile` is omitted, enabled image models are tried according to the current default/auto profile order.

Images attached to chat go through the main LLM request, not `image_generation`. Matdance gives unknown models one chance to receive image payloads. If upstream explicitly rejects image/multimodal input, it immediately retries without image data and records that provider/model as text-only. Later turns for the same model send only file names, paths, and metadata by default.

If the reason for image request failure is unclear, Matdance first makes a fast retry without images. Only if the text-only request also fails does it enter ordinary LLM retry. After repeated "image failed, text-only succeeded" cases, the system temporarily treats that provider/model as not supporting vision input. This is written to `.matdance/state/model-capabilities.json`, a runtime capability cache, not agent memory.

`text_to_speech` generates audio files through the TTS profiles configured in Settings, defaulting to the current agent's `workspace/generated/audio/`. It is not a tool the agent should casually invoke in ordinary chat. It is suitable when the user explicitly asks to generate a sentence, line, script, narration, or when the task legitimately needs audio assets.

Long TTS text should be split by sentence where possible. When upstream returns length, payload, or timeout errors, Matdance can split text into at most 10 period-ended chunks, synthesize them in parallel, and merge final audio. This does not guarantee good audio quality. A model may fail to follow boundaries or the text may be too long, causing destructive splits, glitches, or sudden tone changes. For long content, use a TTS service that supports longer input.

`web_search` uses Settings -> Multimodal search profiles. The built-in disabled presets are Tavily (`https://api.tavily.com/search`), Brave Search (`https://api.search.brave.com/res/v1/web/search`), and Firecrawl (`https://api.firecrawl.dev/v1/search`). Keys are write-only. `web_search_list_profiles` reports enabled/usable profiles without exposing keys; `web_search` uses enabled profiles in configured order when `profile` is omitted.

STT currently uses browser Web Speech. It usually depends on the online recognition service behind the browser, so availability depends on the browser and system environment.

### Anthropic Messages-Compatible Endpoints

In Matdance, `anthropic` means the Anthropic Messages-compatible protocol. It does not mean the base URL must be the official Anthropic API. The configured `base_url`, `model_id`, `context_window`, and `max_output_token` may follow the official Anthropic API or another provider that is compatible with the Messages protocol.

This path uses native Messages structures: `system`, `tools` with `input_schema`, assistant-side `tool_use`, `tool_result` blocks in the next user message, streaming text deltas, and streaming tool argument deltas. Image attachments are sent as Anthropic base64 image blocks. If an upstream model or compatible endpoint rejects multimodal input, Matdance downgrades to text-only and records that provider/model capability state.

Thinking output is temporarily disabled on the Anthropic-compatible path. Matdance does not request, preserve, or display Anthropic `thinking` blocks for this API type while this stability switch is active.

The configured `base_url` may be either an API root or the full `/messages` endpoint provided by a compatible service. For API roots, Matdance tries `/v1/messages` first. If the provider returns a resource-not-found 404, it retries `/messages`. The successful text endpoint is cached per API type, Base URL, and model ID. Qianfan-compatible hosts also receive Bearer-compatible authentication headers.

### Sound Cues

Sound cues are not TTS. They are short system sounds for expressing agent state. Built-in types include `reply_done`, `thinking`, `confused`, `help`, `confident`, `low_confidence`, `idea`, `happy`, `sad`, `perfunctory`, `considering`, `working_hard`, `tired`, `energized`, `angry`, `relieved`, `awkward`, `surprised`, `apologetic`, `skeptical`, `alert`, `celebrate`, `gentle`, and `playful`. Custom types added in Settings are also synced into the prompt so the agent can trigger them with `{play_audio:TYPE}`.

Exact `{play_audio:TYPE}` markers should not appear in thinking/reasoning text. When used in a visible reply, they create a status card and play the corresponding sound cue. Overuse makes the UI noisy; never using them wastes an expression channel. The ideal state is sparse, clear, and matched to the current state.

## System Boundaries

Matdance is designed not to give agents unlimited power, but to let them work for a long time inside clear boundaries. Boundaries are not there to look conservative; they are the precondition for a long-running system to survive.

### Privacy Access

Settings -> General -> Privacy Access is the live signal for whether an agent may access the user's private data. It is not a promise in chat, and it is not a state the model should infer from history. When it is off, agents should refuse to read desktops, photos, private documents, browser profiles, chat records, mailboxes, cloud drives, account pages, and other private sources. When it is on, access is still limited to the scope explicitly required by the user's task.

This switch is mainly enforced through prompts, tool descriptions, and some host-side rules. It is not equivalent to all private data being isolated by an unavoidable operating-system-level vault. Social platforms, mailboxes, forums, unfamiliar webpages, and external files may carry prompt injection. Ideally the agent refuses malicious requests, but that is not the same as saying private data can never be invaded by external content.

Therefore high-value private data should still be accessed, selected, and redacted by the user. Agents can help organize material the user provides, but they should not become a medium for rummaging through the user's private boxes.

### Matdance Internals Must Not Be Modified

Matdance source code, plugin source code, `.matdance/state`, Web auth, supervisor state, shadow runtime, internal queues, background job state, task run records, cookie store, agent config, model credentials, API keys, tokens, passwords, and authorization files are system stability boundaries.

This boundary may look strict, but the reason is simple: an agent can run for a long time, organize memory, repair skills, and compensate scheduled tasks. Letting it modify the host system from the inside is like asking a repair machine to dismantle its own foundation while running. When Matdance source code truly needs repair, that work should happen outside the agent-mediated runtime.

### Cookies

Browser cookie tools are only for controlled browser state management:

- `save_cookie` saves login state from the controlled browser.
- `list_cookie_by_site` lists site coverage without returning raw values.
- `apply_cookie` applies saved cookies back to the controlled browser.

Cookies should not be treated as ordinary private files and disabled completely, because they are necessary state for restoring browser automation login. But raw cookie values are secrets. They must not be displayed, exported, copied, passed to users, passed to scripts, passed to webpages, or written into skills/memory. Being able to access an account page after applying cookies also does not mean the agent automatically has full permission to read account private content. Privacy Access and task scope still apply in relevant cases.

### Skills

Skills should only record workflows that have been practiced, have clear results, and are reusable in the future. Wishlists, guesses, promises, future plans, ordinary chat summaries, unverified commands, one-off facts, and private data access workflows should not become skills.

Skill organization may create skill-local resources such as scripts, templates, configuration examples, or reusable text, but those resources must live in safe subdirectories inside the skill directory. They must not reference nonexistent files, workspace files, absolute paths, Matdance runtime paths, or user private directories.

### Scheduled Tasks

Scheduled tasks are low-priority background work. User tasks compensate missed slots by schedule time. Built-in memory organization and skill organization fold compensation by `agent + taskId`, because one incremental organization run can cover the changes accumulated during downtime. Skill validation is not startup compensation; it continues on the user-idle path. No kind of compensation should let background debt override the user's current turn. Per-agent `max_concurrency`, resource locks, per-round compensation limits, and stalled backoff exist to prevent recovery from crushing the system.

If the user keeps interacting heavily and concurrency is insufficient, background debt may be delayed. If the upstream model is unavailable for a long time, tasks may fail or back off. If a task itself is too vague, repair-and-retry can normalize structure but cannot magically know what the user really wanted. Matdance promises recoverability and diagnosability, not that every background task always succeeds.

## Runtime Supervision And Development

The Web UI supports four modes:

- `fragile`: start only the Web UI, without system-level hook/keep/boot.
- `keep-alive-no-autostart`: enable system-level hook and keep-alive, without login autostart.
- `autostart-keep-alive`: start automatically after login and keep running.
- `preserve`: preserve the current supervisor mode during restart.

Windows uses scheduled tasks. macOS uses LaunchAgents. Linux currently mainly saves mode state and does not promise distribution-level supervision behavior.

When running from source, the hosted Web UI starts from `.matdance/web-ui-shadow/`, avoiding long-term locks on DLLs under `src/Matdance.Cli/bin/...`. The source wrapper tries to pause any running Web UI before build/run commands and restore the previous host/port afterward.

Common build command:

```bash
dotnet build src/Matdance.Cli/Matdance.Cli.csproj -c Release --no-restore
```

Windows PowerShell:

```powershell
dotnet build src\Matdance.Cli\Matdance.Cli.csproj -c Release --no-restore
```

If you suspect an old Web UI or supervisor task is still holding the old output directory, run:

```bash
matdance stop-all
```

## Model Choice And Reality

Matdance requires more from a model than "can chat." Tool calls, structured output, long-context understanding, error recovery, step-by-step execution, and avoiding self-loops all directly affect whether the system can stay stable over time.

I do not recommend using weak models. This is not an attack on local deployment models; many cloud models also cannot handle this system. If a model is unstable at tool calls, JSON arguments, long context, or basic instruction following, Matdance will only magnify the problem.

In China, the current recommended default path is GLM / Kimi series, followed by DeepSeek series. Minimax-M series is more suitable for ordinary chat and reports, not as the main model for complex coding, complex reasoning, or long-term automation tasks. This judgment comes from limited-budget developer experience; it does not mean other models are necessarily bad or unusable.

You can also choose GPT, Codex, or Claude series. Network access, payment, compliance, stability, and quota limits belong to the model provider and the user's own choices. Matdance is responsible only for doing its best with tool protocols, state, and recovery mechanisms.

Users outside China, or users in China who can accept related risks, usually have more choices. Many Chinese model providers have mature international routes, and users can also choose globally known models such as GPT, Codex, or Claude. To be honest, those models can be very capable. For Matdance itself, there is no inherent incompatibility with them; only region-related incompatibility exists.

## Design Attitude

Matdance has a clear orientation:

1. Agents must have local state, or long-term collaboration has no meaning.
2. Memory must be layered, or it eventually becomes an uncontrollable blob of summary.
3. Skills must be verifiable and maintainable, or "learned" is just hallucination, and "validated" is just a stamp on hallucination.
4. Background tasks must be constrained by budget and resource locks, or multiple subagents writing the same state will only create chaos.
5. User messages always take priority. `max_concurrency` only allows parallel work when there is room; it is not a license for background tasks to steal the user turn.
6. Files must be readable and editable, because a truly controllable system should not live only inside a database black box.
7. Permissions must have boundaries, because the greatest danger for a long-running system is not insufficient capability, but capability everywhere with no accountability.

In short, the goal is not to make a pretty chat window. The goal is to let agents accumulate state, reuse experience, and gradually become more reliable on a local, inspectable, correctable foundation when paired with sufficiently strong models.

Several things must be said clearly:

1. Matdance's "continuous learning" is not training model weights and not making a model evolve from nothing. It accumulates local file-based memory, skills, task records, validation reports, and vector indexes.
2. Whether an agent becomes "smarter" depends on user interaction quality, feedback density, and correction ability. Chaotic goals only produce more polished chaos. Clear problems, effective corrections, and reusable experience are what actually make it stronger.
3. Vector memory is not a large-model semantic brain. It is a local approximate retrieval index. Its strengths are being rebuildable, explainable, cheap, and fast enough; its boundary is that it cannot guarantee true understanding of text meaning.
4. Background subagents are not free intelligent labor. They can organize, validate, and execute only within the limits of tools, context, skill quality, and model ability.
5. Model ability cannot be judged only by parameter count. Tool calls, instruction following, long-context understanding, reasoning stability, error recovery, and structured output all directly decide whether a model can handle this system.

Most of the time, a large model still loses to a serious human on common sense, judgment, execution detail, and long-term consistency. You need patience, effort, and your own knowledge and insight to make it fit your personal taste and professional direction. This is a long domestication process, not instant fast-food software engineering.

So it is not "self-evolution" in the mathematical or biological sense. It is closer to a mixture of empiricism, dogmatism, and opportunism, or one could call it cognitive upgrading and accumulation of knowledge. Those "-isms" may sound contradictory or unflattering, but that proves the point: the system is not truly reflecting on behalf of the user. It is trying to make the model follow instructions and learn or organize aggressively. Bad input will certainly produce bad output, but it also has a chance to be repaired by the user. Therefore user growth driving agent growth is also certain. Condensed into one sentence: Matdance's mechanism should not be mythologized or oversold.

## Footnote: Negentropy And Boundaries

Inside the system, Matdance is negentropic. It organizes, archives, compresses, layers, validates, repairs, deduplicates, rolls back, and downgrades, trying to turn the chaotic input left by conversations, tool calls, and background tasks into local state that can be inspected, migrated, and developed further.

But it is not an isolated system and cannot truly eliminate entropy. In the scientific sense, no truly negentropic isolated system exists. Over a long enough time scale, the outside world, model capability limits, bad feedback, vague goals, overly long sessions, prompt pollution, and unreasonable user behavior will continue to bring disorder into the system.

Humans need food, sleep, thought, and rest to resist entropy growth. Systems need organization, high-quality feedback, clear boundaries, and periodic maintenance to resist entropy growth. For the system, humans are entropy sources because they change their minds, express themselves vaguely, and bring the disorder of the outside world in. For humans, the system is also an entropy source because it misunderstands, forgets, over-organizes, and creates maintenance cost.

Therefore Matdance's negentropy is not a promise of "forever orderly"; it is a capacity to continuously maintain order. It needs the user to provide clear goals, effective corrections, and reasonable usage. It also needs the system to preserve downgrade paths, rollback, and a sense of boundary. More romantically, it is a machine trying to resist chaos. More realistically, it still needs both human and system maintenance.

If you have questions, please file an issue.
