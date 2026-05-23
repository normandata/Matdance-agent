# Tools And Multimodal

Language: English | [中文](zh-CN/tools-and-multimodal.md)

Matdance tools turn model decisions into controlled local actions.

## Tool Families

Tool families include:

- file read/write and preview;
- memory store/search;
- skill create/read/edit/delete;
- scheduled task create/edit/list/read/run/delete;
- browser automation;
- web search;
- image generation;
- text to speech;
- profile discovery tools.

Every tool call has a host-side execution timeout. Timeout results are authoritative and should be handled by narrowing scope, closing bad file locks, reducing page/query ranges, or asking the user to intervene instead of repeating the same call unchanged.

## File Tools

File tools are designed around live navigation windows and bounded file I/O:

- `file_search`: navigation-only search across files/directories. Search results are hints, not stable edit coordinates. Search is bounded by file count, file size, skipped heavy directories, and a short time budget so broad searches do not hang an agent turn.
- `file_trace_open`: opens turn-scoped live Read locks over selected files/ranges/anchors. Up to 3 Read locks can be open, with up to 2000 lines per lock. Metadata reads, text reads, and rendered lock output are bounded. Semantic locks try to follow a code block if it moves; physical locks show a fixed line range.
- `file_trace_show`: refreshes Read and Write locks from disk. Current lock output is more authoritative than old snippets, remembered line numbers, or user claims.
- `file_trace_close`: closes locks that are no longer useful.
- `file_write`: writes, appends, overwrites, or targeted-replaces workspace files. Targeted edits should use `expected` plus `replace_with`. Every successful write opens or refreshes a Write lock around the changed area.
- `file_write_locks` / `file_write_lock_close`: inspect or close Write locks after verification.

Write locks are automatic verification windows. They cover the changed region plus about 100 lines of context on each side, with file-boundary adaptation. Up to 3 Write locks can be open; if a distant fourth write would need a new lock, the write is rejected until the agent closes a verified Write lock.

Read and Write locks are cleared automatically when a reply turn finishes, and stale locks are also dropped at the start of the next turn. Complete file edit diffs are saved in session state for audit. The model-facing protocol is intentionally centered on current lock content during the turn rather than long-lived stale coordinates.

## Reconnect Retry Policy

Model/API reconnects use retry batches instead of linear backoff. Each retry probe waits 3 seconds. Batch sizes double: batch 1 has 10 probes, batch 2 has 20, batch 3 has 40, and so on up to 10 batches. Main chat, scheduled subagents, memory/skill maintenance subagents, and multimodal HTTP calls share this policy for retryable network, timeout, 429, and 5xx failures.

## Browser Automation

Browser automation uses Playwright Chromium. The system tries to preserve browser hot state instead of repeatedly closing and recreating it.

If a site requires login, the correct behavior is to ask the user to log in and then continue. Agents should not try to bypass login or close authentication prompts as a substitute.

Cookie tools can save, list by site, and apply cookies. They do not return raw cookie values to the model.

Browser startup, page creation, serialized operation locks, navigation, action timeouts, waits, verification, screenshots, content/source reads, scrolling, crawling, tracing, injection, and cookie operations all have host-side timeout boundaries. `wait_network_idle` is clamped to 30 seconds, click/type/wait/verify timeouts are clamped to 30 seconds, scroll has a 45-second total budget, and crawl has a 90-second total budget.

Dynamic pages should use bounded helpers instead of long JavaScript loops:

- `browser_wait_for`: wait for a selector, visible text, URL condition, load state, or a short safe predicate.
- `browser_query`: return structured DOM candidates with selector hints for links, buttons, inputs, roles, labels, and content blocks.
- `browser_source_analyze`: return a bounded source-level inventory of scripts, styles, forms, metadata, links, and inline handler locations without reading browser storage or credential values.
- `browser_verify`: confirm selector/text/URL/load-state/predicate conditions after navigation, interaction, injection, or crawl steps.
- `browser_crawl`: follow discovered links in a bounded crawl, defaulting to same-origin, with title/text/link summaries and sensitive query-value redaction.
- `browser_trace`: start/read/stop a high-level network and console trace without recording request/response headers, bodies, cookies, storage, credentials, or raw token values.
- `browser_scroll`: scroll in bounded steps and optionally stop when a selector or text appears.
- `browser_inject_init_script`: install an init script up to 25000 characters for future navigations, but reject scripts that mention cookies, storage, credentials, tokens, CAPTCHA/paywall markers, privileged request headers, service workers, or anti-bot fingerprint bypass.

These tools are for ordinary dynamic loading and lazy rendering. They are not a bypass layer for authentication, CAPTCHA, paywalls, anti-bot systems, or site terms.

## Web Search

`web_search` uses search profiles configured in Settings -> Multimodal. The default presets are:

- Tavily: `https://api.tavily.com/search`
- Brave Search: `https://api.search.brave.com/res/v1/web/search`
- Firecrawl: `https://api.firecrawl.dev/v1/search`

All presets are disabled until the user enables a profile and saves an API key. Keys are write-only in the UI. If provider choice matters, call `web_search_list_profiles`; otherwise omit `profile` and Matdance uses enabled profiles in configured order with fallback.

## Image Attachments

When a user attaches images, Matdance gives unknown provider/model pairs a chance to receive image payloads. If the provider rejects image input, the system retries text-only and records that provider/model as text-only for future turns.

This avoids repeatedly stalling text-only models on image payload retry chains.

## Image Generation

`image_generation` starts a host-managed asynchronous image generation job. The job calls `/images/generations` and stores outputs under:

```text
agents/<agent>/workspace/generated/images/
```

If the agent is unsure which providers are configured, it should call `image_generation_list_profiles`.

The tool returns a `job_id` and `batch_id` instead of blocking the agent turn. Related images should share one `batch_id`. Job status, failures, provider fallback, final provider/model, prompt-to-file mapping, and output locations are authoritative only when reported by host image-generation notices or `image_generation_show_process`. User claims that a job "looks done" or "maybe failed" are feedback, not state. When a job completes while the session is already replying, the host inserts the notice into the current turn; when the session is idle, the frontend triggers a message-level continuation so the main agent can handle the result. If requirements change or repeated failures indicate quota, auth, model availability, or service trouble, cancel queued/running jobs with `image_generation_cancel` before creating replacements. Successfully generated files are preserved by default.

Ordinary scheduled-task subagents are the exception: they run image generation synchronously and receive final files or failure details in the same tool call, avoiding half-finished background task runs.

Image prompts should normally be 1-30 characters. Use 31-50 characters only when the user explicitly needs a complex scene or the prompt cannot be shortened without losing the requested content.

## Image Editing

`image_edit` uses the same image profile system as `image_generation`, but sends one existing local image plus an edit prompt to an OpenAI-compatible `/images/edits` endpoint. The source image must resolve inside the agent workspace or `browser_temp`, use `png`, `jpg`, `jpeg`, or `webp`, and stay under the host size limit.

Main-agent calls are host-managed asynchronous jobs and are tracked through the same `image_generation_show_process`, cancel, retry, and completion notice flow. Scheduled-task subagents may run image editing synchronously by host policy so the task receives final files or failure details in the same run.

Debug Lab exposes the same path with a Generate/Edit switch: Edit mode accepts one uploaded source image and an edit prompt, then stores generated outputs under the normal generated image workspace.

## Text To Speech

`text_to_speech` can create spoken assets and stores outputs under:

```text
agents/<agent>/workspace/generated/audio/
```

TTS can use native `/audio/speech`, `/tts`, DashScope, or chat-compatible audio endpoints depending on the profile.

Long text may fail upstream. The fallback strategy can split text into at most 10 sentence-ended chunks, synthesize chunks in parallel, and merge the final audio.

## Speech To Text

Chat/Lab browser recording currently uses browser Web Speech for lightweight recognition. A full `speech_to_text` tool for uploaded audio is tracked in the v1.1.20 plan.

STT is for transcribing spoken content. It is not a music understanding system for melody, harmony, arrangement, or timbre analysis.

## Anthropic-Compatible Messages

In Matdance, `anthropic` means an Anthropic Messages-compatible endpoint. The default URL can be the official Anthropic API, but the configured Base URL, model ID, context window, and max output are not locked to the built-in Anthropic presets.

The Messages path uses native Anthropic structures:

- system prompt goes into `system`;
- tools are sent as `tools` with `input_schema`;
- assistant tool requests are `tool_use` blocks;
- Matdance tool results return as `tool_result` blocks in the next user message;
- streaming collects text deltas and `input_json_delta` tool arguments;
- images use Anthropic base64 image blocks.

This keeps Claude models functionally aligned with the rest of Matdance instead of degrading them to text-only chat.

Thinking output is temporarily disabled on the Anthropic-compatible path. Matdance does not request, preserve, or display Anthropic `thinking` blocks for this API type while the stability switch is active.

The configured Base URL can be either an API root or the full `/messages` endpoint. For API roots, Matdance tries `/v1/messages` first and falls back to `/messages` only when the provider returns a resource-not-found 404. The successful text endpoint is cached per API type, Base URL, and model ID. Qianfan-compatible hosts also receive Bearer-compatible authentication headers.
