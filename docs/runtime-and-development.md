# Runtime And Development

Language: English | [中文](zh-CN/runtime-and-development.md)

This document covers runtime supervision and development checks.

## Source Startup

The source wrappers run the CLI project from the repository root:

Windows:

```powershell
.\matdance.ps1
```

macOS / Linux:

```bash
./matdance
```

The wrappers can pause and restore hosted Web UI services before build/run operations when necessary.

## Hosted Web UI

Recommended hosted mode:

```bash
matdance web-ui start --mode keep-alive-no-autostart --port 8765
```

Public binding uses the same managed wrapper with `--public`; it binds `0.0.0.0`, enables token auth, and prints the token plus token-file path:

```bash
matdance web-ui start --public --port 8765
```

Status:

```bash
matdance web-ui status
matdance web-ui supervisor status
```

Stop:

```bash
matdance web-ui stop
```

Stop all managed Web UI and supervisor tasks:

```bash
matdance stop-all
```

On macOS, `autostart-keep-alive` is registered as a user LaunchAgent under `~/Library/LaunchAgents`. Matdance bootstraps, enables, and kickstarts the LaunchAgent so it survives the next login instead of only existing for the current terminal session.

## Shadow Runtime

Hosted Web UI can run from a shadow directory under `.matdance/web-ui-shadow/`. This reduces source output locking during development builds.

## Build Check

Windows:

```powershell
dotnet build src\Matdance.Cli\Matdance.Cli.csproj -c Release --no-restore
```

macOS / Linux:

```bash
dotnet build src/Matdance.Cli/Matdance.Cli.csproj -c Release --no-restore
```

## Development Notes

- Do not commit `agents/`, `.matdance/`, logs, cookies, API keys, tokens, or generated private workspace files.
- Keep Web UI daily workflows in the browser; keep CLI as launcher and repair path.
- Prefer adding service classes instead of continuing to grow `WebServer.cs` for every feature.
- Test browser, scheduled task, memory, and skill changes with interruption/restart scenarios when possible.
