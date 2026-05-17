# Disclaimer

Language: English | [中文](DISCLAIMER.zh-CN.md)

Matdance is a local-first agent runtime. It is not a hosted cloud service, an enterprise permission platform, a legal/medical/financial advice system, or a privacy vault.

## Model Behavior

Matdance uses prompts, tool descriptions, Settings permissions, host-side restrictions, and local file boundaries to reduce operational risk. The final behavior still depends on the upstream model you configure. Different models vary widely in instruction following, tool-call stability, safety behavior, multimodal support, and recovery from malformed context.

This project cannot guarantee that a model will always be correct, always obey constraints, or never be influenced by external text.

## Private Data

The privacy access switch is the real-time permission signal shown to agents. When it is disabled, agents should refuse to access desktops, photos, private documents, social platforms, mailboxes, private messages, forum account pages, and similar private material. When it is enabled, agents still must not leak passwords, tokens, raw cookies, private originals, or other high-risk values.

Truly sensitive data should be selected, redacted, and handed over by the user directly. Do not treat Matdance as a proxy that can safely manage every privacy boundary for you.

## Browser, Cookies, and Third-Party Services

Browser automation uses Playwright. Matdance can save, list, and apply cookies, but it does not return raw cookie values to the user or model. Writing cookies into a browser context does not guarantee that the current page is logged in or that a third-party service accepts the session.

When you connect third-party model APIs, image generation, TTS, STT, search, websites, or site accounts, you are responsible for understanding and following the relevant terms, billing rules, rate limits, and data handling policies.

## System Stability Boundary

Matdance source code, plugin source code, `.matdance/state`, Web authentication state, supervisor state, shadow runtime directories, run queues, task run records, cookie stores, agent configuration, model credentials, API keys, tokens, passwords, and authorization files are system stability boundaries. Do not use agents as a medium to modify them.

## Platform Support

Matdance primarily targets Windows and macOS. Linux has compatibility work, but distributions, desktop environments, shells, permission policies, and browser dependencies can differ. Mobile, edge, and nonstandard architecture deployments are outside the formally supported scope.

## No Warranty

This project is released under MIT-0. The software is provided as-is, without warranty. You are responsible for the risks of running, modifying, deploying, exposing, connecting third-party services, and processing data with it.
