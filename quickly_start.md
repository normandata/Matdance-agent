# Quick Start

Language: English | [中文](quickly_start.zh-CN.md)

Current version: v1.1.19

This file only covers startup, dependencies, entry registration, and configuration. `README.md` explains what Matdance is, and [FULL-DOC.md](FULL-DOC.md) explains the system in depth.

## Supported Scope

Matdance is currently tested mainly on Windows and macOS. Linux path handling, shell calls, and Playwright dependencies have compatibility work, but distribution-specific behavior is not guaranteed. Mobile devices, edge devices, and unusual architectures are outside the supported deployment guide.

Recommended baseline: at least 4 CPU cores, 4 GB RAM, and 50 GB disk space. A GPU is not required.

## Requirements

Install the .NET 9 SDK. Runtime-only installation is not enough because source startup builds the project.

Windows PowerShell:

```powershell
dotnet --info
```

macOS / Linux:

```bash
dotnet --info
echo "$SHELL"
```

Install .NET 9 SDK from:

```text
https://dotnet.microsoft.com/en-us/download/dotnet/9.0
```

Matdance does not require Java or npm. Playwright uses its own bundled runtime and browser dependency installation.

## Start From Source

Windows PowerShell:

```powershell
dotnet restore src\Matdance.Cli\Matdance.Cli.csproj
.\matdance.ps1
```

macOS / Linux:

```bash
dotnet restore src/Matdance.Cli/Matdance.Cli.csproj
chmod +x ./matdance
./matdance
```

The wrapper starts the CLI from the repository root. It uses the source project under `src/Matdance.Cli/Matdance.Cli.csproj`, so you do not need to type a long `dotnet run` command each time.

Source startup normally uses Debug builds. Release build is a verification step, not the everyday source launcher.

If you do not want the wrapper, the fallback command is:

```bash
dotnet run --project src/Matdance.Cli/Matdance.Cli.csproj -- <command>
```

Windows:

```powershell
dotnet run --project src\Matdance.Cli\Matdance.Cli.csproj -- <command>
```

Check the version:

```bash
./matdance --version
```

Windows:

```powershell
.\matdance.ps1 --version
```

## Wrapper Menu

Run the wrapper with no arguments:

Windows:

```powershell
.\matdance.ps1
```

macOS / Linux:

```bash
./matdance
```

The menu follows the system language. Chinese systems show Chinese by default; other systems show English. The menu also contains explicit `English` and `中文` switches.

Typical menu actions:

- Download dependencies: installs Playwright Chromium.
- Web UI / Supervisor: start, restart, stop, inspect, and configure hosted Web UI modes.
- Install `matdance` entry: registers a user-level command so you can run `matdance` from any directory.
- Language switch: temporarily changes menu language.

The shortest path is:

1. Download dependencies.
2. Open Web UI / Supervisor.
3. Start Web UI in keep-alive without autostart mode.
4. Open:

```text
http://localhost:8765
```

The Web UI is the recommended daily interface. CLI commands remain useful for precise control, scripting, and repair.

## Install Browser Dependencies

Before browser automation, install Playwright Chromium.

Windows PowerShell:

```powershell
.\matdance.ps1 deps install --source global
```

macOS / Linux:

```bash
./matdance deps install --source global
```

For mainland China networks you may try the CN mirror:

```bash
./matdance deps install --source cn
```

Windows:

```powershell
.\matdance.ps1 deps install --source cn
```

Browser dependencies are stored under `.matdance/deps/playwright-browsers` relative to the runtime root. They are not scattered into user profile paths.

## Register the `matdance` Entry

Windows PowerShell:

```powershell
.\matdance.ps1 install-entry --user
```

macOS / Linux:

```bash
./matdance install-entry --user
```

Open a new terminal:

```bash
matdance
```

On macOS/Linux, user installation writes:

```text
~/.local/bin/matdance
```

If `matdance: command not found` appears, add `~/.local/bin` to PATH.

macOS default `zsh`:

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.zprofile"
source "$HOME/.zprofile"
matdance
```

`bash`:

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bash_profile"
source "$HOME/.bash_profile"
matdance
```

If an old entry still points to an old version, reinstall from the source root:

```bash
./matdance install-entry --user
hash -r 2>/dev/null || rehash
which matdance
matdance
```

Source entries remember the current project root and agents root. Running `matdance` from another directory still returns to this source checkout and the same agent data.

## Web UI

Recommended hosted startup:

```bash
matdance web-ui start --mode keep-alive-no-autostart --port 8765
matdance web-ui status
matdance web-ui supervisor status
```

Open:

```text
http://localhost:8765
```

Stop or restart:

```bash
matdance web-ui stop
matdance web-ui restart --port 8765
```

Runtime modes:

- `fragile`: starts Web UI only; no supervisor hook/keep/autostart.
- `keep-alive-no-autostart`: long-running without login autostart; hook and keep-alive enabled.
- `autostart-keep-alive`: login autostart plus keep-alive.
- `preserve`: preserve the current supervisor mode during start/restart.

`web-ui status` reports backend, browser, and dependency state. `web-ui supervisor status` reports hook, keep-alive, and autostart state.

## Remote Web UI

By default, the Web UI should bind only to local loopback hosts: `localhost`, `127.0.0.1`, or `::1`.

If you intentionally bind to `0.0.0.0`, LAN IPs, or other non-loopback hosts, set:

```powershell
$env:MATDANCE_ALLOW_REMOTE_WEB = "1"
$env:MATDANCE_WEB_TOKEN = "replace-with-one-long-random-token"
matdance web-ui start --host 0.0.0.0 --port 8765
```

Remote binding enables single-token authentication. Browser login writes an HttpOnly cookie. API clients may use:

```bash
curl -H "Authorization: Bearer replace-with-one-long-random-token" http://127.0.0.1:8765/api/runtime-status
```

This is not a multi-user account system. Do not expose it to untrusted networks.

## Provider Configuration

Agent model configuration is available in the Web UI Settings/Agent pages and through CLI setup.

Supported API types include:

- `openai_chat`
- `deepseek`
- `zai_glm`
- `zai_glm_coding_plan`
- `baidu_qianfan_coding_plan`
- `xiaomi_mimo`
- `anthropic`

Managed providers fill and lock provider defaults only when the provider definition requires it. OpenAI-compatible endpoints are intentionally flexible. `anthropic` means the Anthropic Messages-compatible protocol; its Base URL, model ID, context window, and max output can follow the official Anthropic API or another compatible provider.

Anthropic uses the native Messages API and supports Matdance tools through `tool_use` and `tool_result` blocks. OpenAI-compatible providers use `/chat/completions` and function-style `tool_calls`.

Thinking output is temporarily disabled on Anthropic-compatible endpoints, matching the current OpenAI-compatible stability policy.

For Anthropic-compatible endpoints, `base_url` may be either the API root or the full `/messages` endpoint provided by a compatible service. For API roots, Matdance first tries `/v1/messages`; if the provider returns a resource-not-found 404, it retries `/messages`. The successful path is cached per API type, base URL, and model ID. Qianfan-compatible hosts also receive Bearer-compatible authentication headers.

## Multimodal Configuration

Global multimodal configuration is stored in:

```text
agents/multimodal_config.json
```

Settings can configure multiple image generation profiles and multiple TTS profiles. API keys are write-only in the UI; leaving a key field blank keeps the existing key.

Supported paths:

- `image_generation`: calls `/images/generations`, stores images under `workspace/generated/images/`.
- `text_to_speech`: calls native `/audio/speech`, `/tts`, DashScope, or chat-compatible audio endpoints, stores audio under `workspace/generated/audio/`.
- `speech_to_text`: Chat/Lab browser recording currently uses browser Web Speech; STT tool work is tracked in the v1.1.20 plan.

TTS long text may be split into sentence-ended chunks and merged when upstream providers reject oversized input.

## Cookie Tools

Browser automation includes:

- `save_cookie`
- `list_cookie_by_site`
- `apply_cookie`

Cookie tools save and apply login state, but they do not return raw cookie values to the model. Cookies are stored under:

```text
agents/<agent>/runtime/browser_cookies/cookies.json
```

This file is sensitive runtime state. Do not commit it.

## Build Check

Windows:

```powershell
dotnet build src\Matdance.Cli\Matdance.Cli.csproj -c Release --no-restore
```

macOS / Linux:

```bash
dotnet build src/Matdance.Cli/Matdance.Cli.csproj -c Release --no-restore
```

If an old Web UI or supervisor process locks build outputs:

```bash
matdance stop-all
```

Then rebuild and restart Web UI if needed:

```bash
matdance web-ui start --mode keep-alive-no-autostart --port 8765
```
