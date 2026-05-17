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

## Browser Automation

Browser automation uses Playwright Chromium. The system tries to preserve browser hot state instead of repeatedly closing and recreating it.

If a site requires login, the correct behavior is to ask the user to log in and then continue. Agents should not try to bypass login or close authentication prompts as a substitute.

Cookie tools can save, list by site, and apply cookies. They do not return raw cookie values to the model.

Dynamic pages should use bounded helpers instead of long JavaScript loops:

- `browser_wait_for`: wait for a selector, visible text, URL condition, load state, or a short safe predicate.
- `browser_query`: return structured DOM candidates with selector hints for links, buttons, inputs, roles, labels, and content blocks.
- `browser_scroll`: scroll in bounded steps and optionally stop when a selector or text appears.
- `browser_inject_init_script`: install a small init script for future navigations, but reject scripts that mention cookies, storage, credentials, tokens, CAPTCHA/paywall markers, network interception, or anti-bot fingerprint bypass.

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

`image_generation` calls `/images/generations` and stores outputs under:

```text
agents/<agent>/workspace/generated/images/
```

If the agent is unsure which providers are configured, it should call `image_generation_list_profiles`.

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
