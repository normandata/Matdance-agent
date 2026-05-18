# Web UI And Security

Language: English | [中文](zh-CN/web-ui-and-security.md)

The Web UI is the recommended Matdance interface. It also defines an important security boundary.

## Binding

The default Web UI should bind only to loopback addresses:

- `localhost`
- `127.0.0.1`
- `::1`

Use loopback binding for normal use. For deliberate LAN/public exposure, use the managed public wrapper:

```bash
matdance web-ui start --public --port 8765
```

The wrapper menu also has `Web UI / Runtime supervisor` -> `Public Web UI / Remote access`, with the same start/restart/supervisor actions as the local Web UI menu but binding to `0.0.0.0`.

The lower-level `web --host 0.0.0.0` command still requires `MATDANCE_ALLOW_REMOTE_WEB=1`; normal operation should go through `web-ui`.

## Remote Authentication

Remote binding enables single-token authentication. You can provide:

```text
MATDANCE_WEB_TOKEN=<long random token>
```

If no token is provided, Matdance generates one, prints it along with the token-file path for public starts/supervisor setup, and stores it under `.matdance/state/web-auth.json`.

Loopback binding stays token-free even when a public token file already exists. This lets you switch back from public exposure to `localhost` without carrying the public login gate into local-only use.

Browser login stores an HttpOnly cookie. API clients can use:

```text
Authorization: Bearer <token>
```

or:

```text
X-Matdance-Token: <token>
```

## What This Is Not

Single-token auth is not a user account system. It does not provide per-agent permissions, per-route roles, audit-grade identity, or multi-user isolation.

Do not expose Matdance to untrusted networks.

## Privacy Switch

The Web UI Settings privacy switch is the live permission authority for private-data access. Agents are instructed to treat the current value as authoritative and to reject private-data tasks when it is off.

This reduces risk, but it does not replace user-side redaction or careful model choice.

## Safe Operation

- Use loopback binding for normal use.
- Use a long random token for remote use.
- Do not reuse common passwords as tokens.
- Avoid remote exposure on public networks.
- Turn off privacy access before working in social platforms, mailboxes, private messages, forums, or unknown webpages.
- Do not paste secrets into screenshots, logs, or issues.
