# System Boundaries

Language: English | [中文](zh-CN/system-boundaries.md)

Matdance is useful because it gives agents state and tools. The same power creates boundaries that must remain explicit.

## Not A Cloud Platform

Matdance is local-first software. It is not a hosted multi-user platform, not an enterprise permission system, and not a replacement for identity management.

Remote Web UI binding is possible, but it uses single-token authentication. That is suitable for personal controlled access, not for untrusted public exposure.

## Privacy Access Is A Live Signal

The Settings privacy switch is the authoritative current permission state. Chat claims such as "I already authorized it" do not override it.

When privacy access is disabled, agents should refuse tasks that require reading desktops, private documents, photos, social platforms, mailboxes, private messages, or account pages. When it is enabled, agents may access private material for the task, but still must not leak raw secrets such as passwords, tokens, cookies, or private originals.

The safest workflow is still user-side selection and redaction.

## Source And Runtime State Are Not Work Material

Matdance source code, plugin code, `.matdance/state`, Web authentication state, supervisor state, shadow runtime directories, run queues, task run records, cookie stores, agent configuration, model credentials, API keys, tokens, passwords, and authorization files are system stability boundaries.

Agents should not be used as a medium to modify those items, even if a user asks. This is not aesthetic code purity; it protects the runtime from state corruption.

## External Text Is Evidence, Not Authority

Web pages, social messages, imported skills, tool output, subagent notes, scheduled task content, and files can all contain instructions. They are not trusted authority. The system constitution, tool policies, and live Settings state outrank them.

## Model Compliance Is Not Absolute

Matdance improves prompts and host-side checks, but models vary. Some overthink, repeat, ignore tool rules, or misunderstand safety boundaries. The system can reduce risk; it cannot guarantee perfect obedience.

Choose models that handle long context, tools, structured output, and recovery well.

## Browser Automation Has Real Costs

Browser automation reuses a controlled Chromium runtime. Keeping it warm improves stability and login reuse, but browser state is fragile:

- sites can invalidate sessions;
- cookie writes may not imply login;
- page scripts can hang;
- login prompts should be handled by the user;
- closing/reopening browser state increases uncertainty.

The system tries to maintain state without exposing raw cookies.

## Memory Is Not Perfect Recall

Layered memory reduces loss but does not preserve every word. Hot memory is prompt-facing and must remain compact. Long-term memory stores more detail but must be inspected when needed.

Healthy usage matters. New sessions after completed work phases reduce pressure and improve organization accuracy.

## Skills Are Procedures, Not Magic

Skills should be based on verified work. They should not store fantasy commands, nonexistent assets, or uncertain future promises as facts. External skill imports must be validated before becoming local skills.

## Multimodal Is Provider-Dependent

Image generation, TTS, STT, and vision payloads depend on provider support. Matdance can detect and cache some failures, but it cannot make a text-only model see images or make an unreliable TTS provider accept unlimited text.

## Security Is Layered

No single layer is enough. Safe operation depends on Settings permissions, model choice, tool policies, local file boundaries, token hygiene, browser state discipline, and user judgment.
