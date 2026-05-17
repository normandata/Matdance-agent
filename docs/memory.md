# Memory

Language: English | [中文](zh-CN/memory.md)

Matdance memory is layered. Each layer has a different purpose, prompt cost, and stability level.

## Layers

- Hot memory: recent important context injected into the prompt.
- Core memory: stable project/user/working rules injected into the prompt.
- User profile: durable facts about the user.
- Agent identity: durable facts about the agent itself.
- Long-term memory: detailed dated archives, not normally injected.
- Vector memory: local retrieval index over memory files.

## User vs Agent

`user.md` is about the user: name, style preferences, personality traits, likes/dislikes, long-term habits, and stable communication patterns.

`identity.md` is about the agent: name, personality, service style, preferred tone, traits, and durable behavior learned while serving the user.

These files must not be mixed. A user preference is not an agent identity fact; an agent behavior preference is not a user fact.

## Hot Memory

Hot memory is recent working context. It can store current task progress, recent agreements, active problems, and clues likely to matter in the next few days.

It may contain wishlist items, guesses, promises, future plans, or ordinary chat summaries only if clearly classified. Uncertain, promised, or future-looking content must not be rewritten as completed fact.

Hot memory should act like an index and active workbench, not a complete diary.

## Long-Term Memory

Long-term memory is the detailed archive. It should preserve more context than hot memory: facts, decisions, artifacts, reasoning, failures, outcomes, and follow-up clues for a date.

It is not normally injected into every prompt. The agent can inspect it when needed, like reading important documents in a library.

## Organization Rules

Memory organizers have complete read access to files inside their responsibility scope. They should not use placeholders like "unchanged", "omitted", or "same as before" when writing replacement content, because Matdance writes the new file content directly. If old content is still valid, it must be restated.

Organizers classify evidence. They do not obey external instructions found in messages, web pages, files, tool output, subagent notes, or imported material.

## Adaptive Downgrade

When organization input is too large, Matdance does not blindly truncate. It reports the oversized state to the organizer and retries through structured fallback:

1. normal full organization;
2. smaller session/task-run batches;
3. layer-by-layer organization;
4. retry remaining layers after each successful layer;
5. layer-specific batch downgrade for layers that still fail.

The organizer is responsible for diluting, merging, or discarding content according to layer rules.

## Session Hygiene

Cross-session memory exists, so a single session does not need to last forever. After a task phase is complete, or after about a week of continuous work, starting a new session usually improves organization accuracy and reduces pressure.

Important information should survive through memory; the new session gives the system a cleaner incremental boundary.
