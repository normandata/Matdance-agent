# 运行守护与开发说明

Language: [English](../runtime-and-development.md) | 中文

Matdance 的源码入口和托管 Web UI 都围绕长期运行做了处理。目标是在开发、重启、休眠和后台任务补偿之间保持可恢复状态。

这部分文档只讨论运行守护、构建和开发边界。Matdance 的设计细节见 [系统在做什么](system-overview.md)，权限边界见 [系统边界与代价](system-boundaries.md)。

## 运行模式

Web UI 支持四类模式：

- `fragile`：只启动 Web UI，不启用系统级 hook/keep/boot。终端关闭或进程退出后不会自动恢复。
- `keep-alive-no-autostart`：启用系统级 hook 和 keep-alive，但不开机自启。
- `autostart-keep-alive`：登录后自动拉起，并持续保持运行。
- `preserve`：重启时保留当前守护模式。

Windows 使用计划任务，macOS 使用 LaunchAgents。Linux 目前主要保存模式状态，不承诺发行版级守护体验。

macOS 的 `autostart-keep-alive` 会注册到当前用户的 `~/Library/LaunchAgents`。注册时会写入 plist、设置权限、执行 `launchctl bootstrap`、`launchctl enable`，并在需要时 `kickstart`，这样它不是只在当前终端会话里临时存在，而是应在下一次用户登录后继续拉起 Web UI。

公网/局域网暴露也走托管包装入口：

```bash
matdance web-ui start --public --port 8765
```

它会绑定 `0.0.0.0`，启用 token 鉴权，并打印 token 与 token 文件位置。

## Shadow 运行目录

源码运行时，托管 Web UI 会从 `.matdance/web-ui-shadow/` 启动，避免长期占用 `src/Matdance.Cli/bin/...` 中的 DLL。源码包装入口在执行 build/run 前会尽量暂停正在运行的 Web UI，命令结束后再恢复原 host/port。

## 构建检查

常用构建命令：

```bash
dotnet build src/Matdance.Cli/Matdance.Cli.csproj -c Release --no-restore
```

Windows PowerShell：

```powershell
dotnet build src\Matdance.Cli\Matdance.Cli.csproj -c Release --no-restore
```

如果怀疑旧 Web UI 或守护任务仍在占用旧输出目录，可以先执行：

```bash
matdance stop-all
```

它会关闭 Web UI，并禁用 hook、keep-alive 和开机自启。

## 开发边界

agent 运行时不应修改 Matdance 源码、插件源码、`.matdance/state`、Web auth、supervisor 状态、运行队列、任务运行记录、cookie store、agent config 或凭据文件。Matdance 维护应在 agent-mediated runtime 之外完成。

这条规则不是代码洁癖。Matdance 允许 agent 维护记忆、技能、工作区和定时任务，这已经足够影响长期行为；如果再允许 agent 从内部改宿主源码和运行状态，调试边界会迅速消失。用户明确要求也不应该通过 agent runtime 修改这些文件。真正的系统维护应该由 Matdance 的开发者或社区开发者在普通开发流程里完成：读代码、改代码、构建、测试、提交。

允许 agent 写入的区域应该是任务工作区、用户明确授权的非敏感文件、技能目录里的 skill-local 资源，以及系统工具暴露的受控 API。禁止区包括源码、凭据、cookie 明值、Web token、supervisor 状态、active run、heartbeat、stalled/backoff 记录和内部队列。

## 时间语义

会话时间、后台事件、任务运行记录、记忆整理上下文、文件时间和 cookie store `savedAt` 会按用户时区写出并带 offset。session id、run id、调度去重、cookie expires、Web token expires 和进程超时仍使用 UTC/Unix 时间。

这种混合设计看起来麻烦，但它区分了两个问题：人读到的时间应该符合用户时区，系统判断唯一性、过期和超时则应该使用稳定边界。不要把两者混在一起排查，否则很容易把“用户看到的北京时间”和“内部 UTC 去重边界”误认为冲突。

