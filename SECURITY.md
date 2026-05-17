# Security Policy

Language: English | [中文](SECURITY.zh-CN.md)

Matdance security is built from local file layout, Web UI binding policy, single-token authentication, Settings permissions, tool descriptions, prompt constraints, and host-side limits. These reduce risk, but they do not turn an arbitrary upstream model into an absolutely reliable secure execution environment.

## Supported Versions

Only the main branch and the latest preview line are actively maintained. Older versions may lack the latest permission prompts, browser automation restrictions, cookie diagnostics, or background task recovery logic.

## Web UI

By default, the Web UI should bind only to `localhost`, `127.0.0.1`, or `::1`. If you explicitly enable remote binding, Matdance enables single-token authentication. This is not a multi-user permission system. Do not expose the Web UI to untrusted networks.

## Secrets

Do not paste API keys, tokens, cookies, passwords, private messages, mailbox content, or other sensitive originals into issues, logs, screenshots, or public documents. Redact first.

## Reporting

If you find a security issue, report the smallest reproduction and boundary you can. Do not attach real credentials or private data. If no private report channel is available, public issues should include only impact, version, reproduction steps, and redacted logs.

## Practical Guidance

- Turn off privacy access before working inside social platforms, mailboxes, private messages, forums, unknown webpages, or other third-party text-dense environments.
- Use a long random token when remotely exposing the Web UI, and do not reuse common passwords.
- Do not let agents modify Matdance source code, runtime state, authentication state, cookie stores, model credentials, or supervisor state.
- Browser cookie tools are for reasonable session reuse, not for exporting private data.
