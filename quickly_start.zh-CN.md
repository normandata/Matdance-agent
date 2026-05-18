# Quickly Start

Language: [English](quickly_start.md) | 中文

当前版本：v1.1.20-preview

v1.1.20-preview 的重点是 Web UI 公网包装、macOS 部署修复、浏览器工具链扩展、提示音与技能/记忆整理边界收敛，以及长期运行状态在本机和 public 模式之间的可切换性。Web UI 托管进程仍会从 shadow 目录启动，源码入口在需要 build/run 前会尽量暂停并恢复占用服务。

这份文件只放启动、依赖、入口注册和配置。README 负责解释 Matdance 是什么，这里负责让它跑起来。别把两种文档搅在一起。

## 支持范围

开发者不提供任何移动设备、边缘设备、异架构设备部署指南。这几类平台也许能跑，但没有经过验证；这份指南应该由酷爱折腾的社区开发者提供，而不是由我在这里硬装全平台专家。

目前经过一定测试的平台是 Windows 和 macOS。Linux 的路径处理、shell 调用和 Playwright 依赖都在代码里做了兼容，理论上可用，但文档暂时只给通用命令，不承诺 Ubuntu、Debian 或其他发行版的细节体验。后续真测过，再把话说满。

通常情况下，Matdance 的性能开销不算夸张。推荐配置是至少 4 个 CPU 核心、4 GB 运行内存和 50 GB 以上磁盘空间。它没有显卡硬要求，显卡最多也就是浏览器页面渲染时选择性加速一下；弱于这个配置通常也能跑，只是长时间运行时更容易被系统内存策略教育，尤其是 Windows。

## 准备环境

先安装 .NET 9 SDK。

Matdance 的推荐入口是 Web UI。CLI 是启动器、维修口和少量低层命令集合，不是日常聊天界面。日常聊天、配置、记忆、技能、定时任务、向量图册和 Lab 调试都应该走 Web UI。

Windows PowerShell 先确认 .NET：

```powershell
dotnet --info
```

macOS / Linux 再顺手看一下 shell：

```bash
dotnet --info
echo "$SHELL"
```

真正必须存在的是 `dotnet --info`；如果它不存在，先把 .NET 9 SDK 装好。

只装 Runtime 不够，因为源码启动需要构建项目；装 SDK 就行，SDK 会带运行时。去官网装 .NET 9 这一条线的 SDK：`https://dotnet.microsoft.com/en-us/download/dotnet/9.0`。

macOS 默认 shell 通常是 `zsh`，后面 PATH 示例也按 `zsh` 写；如果你用的是 `bash`，看注册入口那一节。

## 从源码启动

Windows PowerShell：

```powershell
dotnet restore src\Matdance.Cli\Matdance.Cli.csproj
.\matdance.ps1
```

`.\matdance.ps1` 本质上是源码包装脚本，会从仓库根目录执行 `dotnet run -c Debug --project src\Matdance.Cli\Matdance.Cli.csproj -- ...`。它默认跑 Debug 构建，不是 Release；Release 构建放到最后的“构建检查”里，不要把启动和验收搅成一坨。包装脚本在触发 `dotnet run` 前会先探测托管 Web UI，必要时暂停服务，命令结束后再按原 host/port 恢复，避免后台进程锁住 Debug 输出目录。

不带参数会进入 Matdance 主菜单。菜单上方会显示 Web UI 是否正在运行、运行时间、当前守护模式、hook/keep/boot 状态、当前 OS、shell 工具和 agents 根目录；下面可以选择下载依赖、进入 Web UI / 运行守护二级菜单、注册 `matdance` 入口。

macOS / Linux：

```bash
dotnet restore src/Matdance.Cli/Matdance.Cli.csproj
chmod +x ./matdance
./matdance
```

`./matdance` 是 macOS/Linux 下和 Windows `.\matdance.ps1` 对齐的源码启动器。它会从仓库根目录调用当前源码里的 `src/Matdance.Cli/Matdance.Cli.csproj`，不需要你每次手写一大串 `dotnet run`。

源码状态下，macOS 用户在注册 `matdance` 前，后续所有命令都用这个前缀：

```bash
./matdance <command>
```

也就是说，Windows 文档里的：

```powershell
.\matdance.ps1 deps install --source global
```

在 macOS 上就是：

```bash
./matdance deps install --source global
```

如果你不想用包装脚本，原始 fallback 仍然是：

```bash
dotnet run --project src/Matdance.Cli/Matdance.Cli.csproj -- <command>
```

需要看当前版本时可以用：

```bash
./matdance --version
```

Windows 对应就是：

```powershell
.\matdance.ps1 --version
```

## 包装菜单

如果只是想最快跑起来，不需要先背命令。直接运行：

Windows PowerShell：

```powershell
.\matdance.ps1
```

macOS / Linux：

```bash
./matdance
```

它会打开一个包装菜单。菜单默认跟随你的系统语言，中文系统显示中文，其他系统默认显示 English；菜单里也固定有 `English` 和 `中文` 两个选项，临时切换不用去翻配置文件。

中文菜单大概长这样：

```text
选择操作

> 1. 下载依赖
  2. Web UI / 运行守护
  3. 注册 matdance 入口
  4. English
  5. 中文
  0. 退出
```

这几个选项对应的事情很直接：

- `下载依赖`：安装 Playwright Chromium，第一次使用浏览器自动化前先跑它。
- `Web UI / 运行守护`：进入二级菜单，里面可以查看运行状态、以“可能断连”模式启动 Web UI、以“长期运行但不开机自启”模式启动 Web UI、重启 Web UI、停止 Web UI、启用/禁用开机自启、手动跑一次守护 hook，或者关闭全部 Web UI / hook / keep / boot。
- `注册 matdance 入口`：把 `matdance` 命令注册进 PATH，以后就不用每次都从项目目录敲./matdance包装脚本，而是直接使用 `matdance`,前者适用于在项目根目录下运行，后者适用于所有路径，通常情况下，一旦完成注册，打开终端后直接输入 `matdance`就可以进入包装脚本界面。
- `English` / `中文`：切换当前菜单语言。

所以最短路径其实是：先选 `1. 下载依赖`，再进 `2. Web UI / 运行守护`，选择“启动 Web UI（长期运行，不开机自启）”，然后浏览器打开：

```text
http://localhost:8765
```
到了这里，几乎也就没什么可讲的了，包装脚本提供了快速便捷的启动方法，后面只剩下 CLI 命令合集了，如果需要就可以接着往下看。

CLI 命令仍然保留，是给你需要精确控制、写脚本或者排查问题时用的。日常启动别硬背命令，菜单就是为了少折腾。

## 安装依赖

首次使用浏览器自动化前，先安装 Playwright Chromium。国内网络可以先试 CN 源，失败时安装器会回退到官方源：

Windows PowerShell：

```powershell
.\matdance.ps1 deps install --source cn
```

国际通用链路：

```powershell
.\matdance.ps1 deps install --source global
```

macOS / Linux：

```bash
./matdance deps install --source global
```

国内网络也可以试 CN 源：

```bash
./matdance deps install --source cn
```

当前浏览器依赖使用 Playwright `1.59.0`。依赖会安装到程序根目录下的 `.matdance/deps/playwright-browsers`，不会塞到用户 profile 里让你到处猜。

macOS 上如果旧版本安装时看起来一直停在 `0%`，通常是 Playwright 用回车刷新同一行进度，而旧日志读取方式没有持续打印这些刷新。新版本会正常输出这类进度更新。若几分钟仍没有任何变化，先在 `--source cn` 和 `--source global` 之间切换；必要时删除未完成的 `.matdance/deps/playwright-browsers` 后重试。从压缩包解压后如果 `./matdance` 无法执行，先运行 `chmod +x ./matdance`；如果 macOS 隔离标记导致无权限或无法打开，在确认来源可信后可对 Matdance 目录执行 `xattr -dr com.apple.quarantine <Matdance目录>`。

Apple Silicon 建议安装 arm64 版 .NET 9。若在 Rosetta 下运行 x64 的 `dotnet`，Matdance 会跟随进程架构使用 x64 Playwright Node，通常能跑，但不是首选路径。

需要注意，“运行时根目录”不是永远等于程序输出目录。源码包装脚本会从仓库根目录运行，通常使用仓库根目录下的 `.matdance/`；发布包或从别的工作目录直接运行 DLL 时，会按当前项目/agents/程序根目录重新推导。`.matdance/` 里会放依赖、Playwright 浏览器、Playwright 驱动缓存、Web UI 状态、Web 鉴权 token、用户时区状态、注册入口脚本和 Web UI 影子运行目录。

浏览器自动化工具链里现在有三类 cookie 工具：`save_cookie`、`list_cookie_by_site`、`apply_cookie`。默认行为都是全量保存或全量应用；可选的 `site` 参数只用于过滤主域分组。分组会把子域折到主域下面，比如 `mail.example.com` 和 `example.com` 都归到 `example.com`。cookie 保存到当前 agent 的 `agents/<agent>/runtime/browser_cookies/cookies.json`，`savedAt` 会按 Matdance 用户时区写入；cookie 自身的 `expires` 仍是浏览器标准 epoch 秒。这个文件属于敏感运行时状态，不要提交、不要拿去预览。

## 注册入口

注册当前用户入口：

Windows PowerShell：

```powershell
.\matdance.ps1 install-entry --user
```

macOS / Linux：

```bash
./matdance install-entry --user
```

打开新终端后：

```bash
matdance
```

在 Windows 上，不带 `--user` 时如果当前进程有管理员权限，会优先写 machine PATH；否则写 user PATH。

macOS / Linux 的 `--user` 会把入口放到：

```text
~/.local/bin/matdance
```

如果新终端里提示 `matdance: command not found`，说明 `~/.local/bin` 不在 PATH。macOS 默认 `zsh` 可以这样补：

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.zprofile"
source "$HOME/.zprofile"
matdance
```

如果你用的是 `bash`：

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> "$HOME/.bash_profile"
source "$HOME/.bash_profile"
matdance
```

这两段 PATH 命令只需要加一次。你要是反复追加十几遍，shell 不会坏，但配置文件会显得很有精神问题。

不带 `--user` 时，macOS/Linux 会优先尝试 `/usr/local/bin/matdance`，不可写时落到 `~/.local/bin/matdance`。如果你想写 `/usr/local/bin`，就需要当前用户有写权限；别为了一个启动脚本盲目 `sudo` 整个源码构建流程，最后留下 root 拥有的 `bin/obj`，排查起来很不体面。

如果你很早以前注册过 `matdance`，现在运行 `matdance` 却还是旧版本，先在源码根目录重新注册：

```bash
./matdance install-entry --user
```

然后开一个新终端，或者让当前 shell 刷新命令缓存：

```bash
hash -r 2>/dev/null || rehash
which matdance
matdance
```

新注册逻辑会尽量修复 PATH 中可写的旧 Matdance 启动脚本；如果旧入口在 `/usr/local/bin/matdance` 这类当前用户不可写的位置，终端会提示哪些旧入口没能更新。那种情况就需要你删除旧入口，或者用有权限的终端重新注册。不要靠祈祷 PATH 自己变聪明，它没有这项功能。

还有一个源码运行时的小细节：注册出来的 `matdance` 入口会记住当前项目根目录和当前 `agents` 根目录。以后你从别的目录敲 `matdance`，它也会先回到这个源码项目，再使用这套 `agents`，不会因为当前终端目录不同就新建一套空数据。源码入口会通过 `dotnet run` 跑当前项目，所以代码更新后通常能跟上；如果 Web UI 正在运行，源码入口会在 build 前临时暂停它，命令完成后再恢复。`web-ui stop/status/stop-all` 等管理命令会直接调用已构建 DLL，不会为了进入子命令先触发一次 build。

## Web UI

注册入口之后，推荐用长期运行但不开机自启的托管模式启动 Web UI：

```bash
matdance web-ui start --mode keep-alive-no-autostart --port 8765
matdance web-ui status
matdance web-ui supervisor status
```

打开：

```text
http://localhost:8765
```

Web UI 默认应该只绑定本机地址。`localhost`、`127.0.0.1` 和 `::1` 会直接允许。需要主动暴露到局域网或公网时，优先使用 `web-ui start --public`，它会绑定 `0.0.0.0`、开启单 token 鉴权、生成或读取 `.matdance/state/web-auth.json`，并把 token 路径和当前 token 打印到控制台。浏览器登录成功后会写入 HttpOnly cookie，系统 API 可以用 `Authorization: Bearer <token>` 或 `X-Matdance-Token`。你也可以提前设置 `MATDANCE_WEB_TOKEN` 来使用自己的长随机 token。

交互式包装菜单也有同一条路径：`Web UI / 运行守护` -> `Public Web UI / Remote access`。这个子菜单和本地 Web UI 菜单的启动、重启、守护选项一致，只是绑定公网地址，并会在 public 启动或守护配置时打印 token 信息。

PowerShell 远程暴露示例：
```powershell
$env:MATDANCE_WEB_TOKEN = "replace-with-one-long-random-token"
matdance web-ui start --public --port 8765
```

macOS / Linux bash/zsh 远程暴露示例：
```bash
export MATDANCE_WEB_TOKEN="replace-with-one-long-random-token"
matdance web-ui start --public --port 8765
```

如果你明确需要手动绑定某个局域网 IP，仍可使用 `--host <ip>`；非 loopback host 会自动走远程鉴权路径。直接运行底层 `web --host 0.0.0.0` 时仍需要手动设置 `MATDANCE_ALLOW_REMOTE_WEB=1`，日常不建议这样绕过 `web-ui` 包装。

切回 `localhost`、`127.0.0.1` 或 `::1` 这类本机绑定时，即使 public token 文件还存在，也不会继续要求 Web UI token。公网模式和本机模式应该可以来回切换，不互相污染。

API 调用示例：
```bash
curl -H "Authorization: Bearer replace-with-one-long-random-token" http://127.0.0.1:8765/api/runtime-status
```

Web UI 首次打开时也会跟随浏览器/系统语言。中文环境默认中文，其他环境默认 English；如果你在 Settings 里点了语言切换，才会把选择写进浏览器本地存储。这样不需要用户一进来就在陌生语言里到处找 Settings。

停止或重启 Web UI：

```bash
matdance web-ui stop
matdance web-ui restart --port 8765
```

托管模式会拉起后端服务，并预热浏览器运行时。`web-ui status` 会显示 backend/browser/deps 状态，不要再把“只开了前端页面”误当成 Web UI 已经完整启动。`web-ui supervisor status` 会显示当前守护模式，以及 hook、keepAlive、autostart 是否启用。

托管进程状态文件会记录 PID、启动时间、host/port、agents 根目录，以及可执行文件路径或进程名。停止 Web UI 时不会只凭 PID 杀进程，会先核对这些身份信息，避免系统 PID 复用后误杀不属于 Matdance 的进程。

运行模式有四类：

- `fragile`：可能断连的运行方式。只启动 Web UI，不启用系统级 hook/keep/boot；关闭终端、系统策略回收或进程退出后不会自动恢复。
- `keep-alive-no-autostart`：长期运行但不开机自启。会启用系统级 hook 和 keep-alive；Web UI 掉线后会被重新拉起，错过的到期任务也能由 hook 在后台补偿。
- `autostart-keep-alive`：开机自启并长期运行。除 hook 和 keep-alive 外，还会注册登录时自动拉起 Web UI。
- `preserve`：启动或重启 Web UI 时保留当前守护模式。

如果只想启用或调整守护任务，也可以直接用：

```bash
matdance web-ui supervisor enable
matdance web-ui supervisor enable --autostart
matdance web-ui supervisor disable
matdance web-ui supervise --run-due
```

Windows 下守护任务由计划任务运行，命令会通过隐藏的 `wscript.exe` 包装脚本执行，避免周期性弹出控制台窗口。macOS 下使用当前用户的 LaunchAgents。Linux 目前只会写入模式状态，不承诺系统级守护。

Web UI 托管进程重启、版本更新、电脑休眠之后，定时任务不会简单跳过错过的触发点。后台 worker 或系统级 hook 恢复后会按原计划时间补偿到期任务；一天内多次触发的规则会逐个补偿 missed slots，并用执行记录里的 `scheduledAt` 避免重复执行。为了避免长时间停机后一次性堆爆，每个任务单轮最多补偿 8 个触发点，每个 agent 单轮最多取 25 个到期项；系统级 hook 前台补偿仍然限制单次最多执行 25 个任务。新的定时任务通知投递到当前聊天页时会播放一次回复完成提示音，历史加载不会反复响。补偿这一块更像一种债务，遵循用户优先策略，可能会被持续交互拖欠；这种情况不是系统坏了，而是系统刻意把用户回合放在后台债务之前。

后台调度按 agent 分区计算并发预算，默认 `max_concurrency=1`。优先级是记忆整理 > 技能整理 > 用户创建的定时任务 > 技能验证。用户消息和 Web UI 手动触发的记忆整理、技能整理、技能验证、学习并验证、手动执行定时任务同属最高优先级：它们会注册成前台租约、占用用户预算，并让低优先级后台任务取消或等待；`max_concurrency=2` 时一个用户回合占 1 个槽，后台仍可同时跑 1 个任务。记忆和技能这类共享资源会额外上锁；拿不到锁时先跑其它任务，本轮没有其它可跑任务时，每次资源锁等待最多 30 秒，下一轮会重新排序、重新计算预算并继续尝试，不是全局总等待上限。聊天工具里的 `scheduled_task_do` 也走同一套 `BackgroundWorkCoordinator` 和资源锁，不会绕过共享文件边界。

调度默认跟随浏览器时区，并把最近一次上报的时区保存到 `.matdance/state/user-time-zone.json`；需要强制固定时可以设置 `MATDANCE_TIME_ZONE`。一次性任务必须选未来时间，创建或启用时如果没有未来触发点会被拒绝。已经到期但还没执行的 `nextRunAt` 会保留为 due 游标，不会因为启动时重建系统任务、打开列表或读取详情就被推进到未来；只有 worker 真正执行并写入运行记录后才会按结果推进。

调度任务拿到预算和资源锁后会立刻持久化 active run，并写入 `scheduled_tasks/runs/<task-id>/<run-id>.json`。运行期间模型请求、模型重试、工具调用、子任务阶段变化和通知投递都会刷新心跳。网络或模型访问问题先按主 agent 的 LLM retry 规则自动重试，并把 `llm_retry_wait`、错误类型和最后一次心跳写进诊断；只有任务真正开始执行后连续 10 分钟没有任何心跳，才会标记为 `stalled`，进入 30 分钟退避并排到最低恢复优先级。Schedule 页面会给异常任务提供“重试”和“修复并重试”：前者清掉退避立即入队，后者会克隆并规范化任务结构、把旧活动项标记为 `replaced`，保留同一个任务 ID 和运行历史再入队。

会话 JSON、消息时间、书签、后台事件、任务运行记录、记忆/技能整理上下文、Web memory API 文件时间和 cookie store `savedAt` 都会按用户时区或任务时区写出并带 offset。不要把这些和内部 UTC 边界混在一起：session id、run id、调度 due/catch-up 去重、cookie expires、Web token expires 和进程超时仍使用 UTC/Unix 时间。

如果需要把开发环境彻底清干净，或者准备重新配置守护模式，用：

```bash
matdance stop-all
```

它会关闭 Web UI，并禁用 hook、keep-alive 和开机自启。也可以用等价的 `matdance web-ui stop-all`。源码包装脚本对 `stop-all` 有快路径，会直接调用已构建 DLL，不会先触发 `dotnet run`。普通源码命令则会自动暂停/恢复正在运行的 Web UI；托管 Web UI 自身会从 `.matdance/web-ui-shadow/` 运行，避免长期占用 `src/Matdance.Cli/bin/...` 里的 DLL。

后台任务状态可以在 Settings -> General 的“后台事件”里看，并且可以在面板内选择要查看的 agent。它会同步最近的 subagent、调度和恢复事件，并按已完成、未完成、已跳过、失败和剩余未完成项汇总。这里不是一个装饰列表；如果记忆整理、技能整理、技能验证或定时任务失败，它会给出手动重试、检查 API key/网络、查看记忆或技能报告这类补救建议。旧进程留下的 `runtime/jobs` 未完成项会在 Web UI worker 启动时被恢复成 `interrupted` 事件，避免幽灵作业长期占住“剩余未完成项”。

如果只想在当前终端前台运行 Web 服务：

Windows PowerShell：

```powershell
.\matdance.ps1 web --port 8765
```

macOS / Linux：

```bash
./matdance web --port 8765
```

注册入口以后，macOS 也直接用：

```bash
matdance web --port 8765
```

如果你还没注册入口，就把上面所有 `matdance` 换成当前平台的源码包装脚本。Windows 用 `.\matdance.ps1`，macOS/Linux 用 `./matdance`。别在 PATH 没配好时和终端较劲，终端不会因为你盯着它看就突然懂事。

## 常用 CLI

Windows PowerShell：

```powershell
.\matdance.ps1 agent list
.\matdance.ps1 agent create 菲菲 --model gpt-5.5 --base-url https://example.com/v1 --api-key sk-xxx --api-type openai_chat
.\matdance.ps1 agent create 菲菲 --model deepseek-v4-flash --api-key sk-xxx --api-type deepseek
.\matdance.ps1 agent config 菲菲 --edit
.\matdance.ps1 agent identity 菲菲
.\matdance.ps1 agent user 菲菲

.\matdance.ps1 session list 菲菲
.\matdance.ps1 session show 菲菲 <session-id>

.\matdance.ps1 memory hot 菲菲
.\matdance.ps1 memory hot 菲菲 --edit
.\matdance.ps1 memory core 菲菲 --edit
.\matdance.ps1 memory long 菲菲
.\matdance.ps1 memory long 菲菲 2026-05-09
.\matdance.ps1 memory vector 菲菲
.\matdance.ps1 memory search 菲菲 "技能 验证" --take 5

.\matdance.ps1 workspace tree 菲菲
.\matdance.ps1 workspace open 菲菲

.\matdance.ps1 deps install --source cn
.\matdance.ps1 web-ui start --mode keep-alive-no-autostart --port 8765
.\matdance.ps1 web-ui status
.\matdance.ps1 web-ui supervisor status
.\matdance.ps1 web-ui stop
.\matdance.ps1 stop-all
.\matdance.ps1 install-entry --user
```

macOS / Linux 源码运行时：

```bash
./matdance agent list
./matdance agent create 菲菲 --model gpt-5.5 --base-url https://example.com/v1 --api-key sk-xxx --api-type openai_chat
./matdance agent create 菲菲 --model deepseek-v4-flash --api-key sk-xxx --api-type deepseek
./matdance agent config 菲菲 --edit
./matdance agent identity 菲菲
./matdance agent user 菲菲

./matdance session list 菲菲
./matdance session show 菲菲 <session-id>

./matdance memory hot 菲菲
./matdance memory hot 菲菲 --edit
./matdance memory core 菲菲 --edit
./matdance memory long 菲菲
./matdance memory long 菲菲 2026-05-09
./matdance memory vector 菲菲
./matdance memory search 菲菲 "技能 验证" --take 5

./matdance workspace tree 菲菲
./matdance workspace open 菲菲

./matdance deps install --source global
./matdance web-ui start --mode keep-alive-no-autostart --port 8765
./matdance web-ui status
./matdance web-ui supervisor status
./matdance web-ui stop
./matdance stop-all
./matdance install-entry --user
```

注册入口以后，macOS / Linux 不需要再背这么长的 `dotnet run` 前缀，直接用：

```bash
matdance agent list
matdance web-ui start --mode keep-alive-no-autostart --port 8765
matdance web-ui status
matdance web-ui supervisor status
matdance web-ui stop
matdance stop-all
```

如果项目不在默认位置，或者要使用另一个 agent 数据根目录：

```powershell
.\matdance.ps1 --agents-dir E:\my_agents web-ui start --mode keep-alive-no-autostart --port 8765
```

macOS/Linux：

```bash
./matdance --agents-dir /Users/me/matdance-agents web-ui start --mode keep-alive-no-autostart --port 8765
```

## .matdance 运行时目录

`.matdance/` 是本机运行目录，不属于源码资产。源码包装脚本通常使用仓库根目录下的 `.matdance/`，发布包或直接从其他目录运行 DLL 时会按当前项目/agents/程序根目录推导自己的 `.matdance/`。

典型结构：

```text
.matdance/
  deps/
    playwright-browsers/
  state/
    supervisor/
  bin/
```

- `deps/`：依赖安装器下载的运行时依赖，目前主要是 Playwright Chromium。
- `state/`：托管 Web UI 的进程状态、单 token Web 鉴权状态、运行守护状态和隐藏执行脚本。进程状态包含 PID 和身份校验信息，用于避免停止命令误杀 PID 复用后的其他进程。
- `web-ui-shadow/`：源码运行时托管 Web UI 的影子输出目录。后台进程从这里加载 CLI 和插件 DLL，不再长期锁住 Debug/Release 构建输出目录。
- `deps/playwright-driver/`：Playwright .NET 驱动缓存。浏览器二进制仍然放在 `deps/playwright-browsers/`，驱动缓存单独放置是为了让影子运行目录保持轻量。
- `bin/`：`install-entry` 生成的 `matdance` 启动脚本或 Windows `matdance.cmd`/`matdance.ps1`。

## Agent 配置

`agents/<agent>/config/agent_config.json` 保存模型连接与运行参数：

- `name`：agent 名称，通常与目录名一致。
- `base_url`：模型服务地址。
- `model_id`：模型 ID。
- `api_key`：API key，属于敏感信息，不应该提交。
- `api_type`：接口类型。`openai_chat` 走 OpenAI-compatible `/chat/completions`；`deepseek`、`zai_glm`、`zai_glm_coding_plan`、`baidu_qianfan_coding_plan`、`xiaomi_mimo` 会使用内置 provider 预设并锁定对应 Base URL、模型和额度预设；`anthropic` 走 Anthropic Messages 兼容协议，支持 `tool_use` / `tool_result` 工具块，`base_url`、`model_id`、`context_window` 和 `max_output_token` 可以按官方 API 或其它兼容提供商填写。
- `context_window`：上下文窗口大小。
- `max_output_token`：单次最大输出 token。
- `max_concurrency`：该 agent 的并发预算，默认 1，允许范围 1-16。用户消息、Web UI 手动整理/验证/执行、后台定时任务、记忆/技能整理和技能验证都从这里扣预算；普通用户使用建议先保持 1，需要用户回合和后台任务同时跑时再调到 2。
- `temperature`：采样温度。
- `compression_threshold`：上下文压缩触发阈值，为 0.0-1.0 之间的比例值。
- `hot_memory_limit`、`core_memory_limit`、`user_md_limit`、`identity_md_limit`：记忆与画像文件的建议 token 限制。

Anthropic-compatible 的 `base_url` 可以填 API 根地址，也可以填兼容服务给出的完整 `/messages` 端点。对于 API 根地址，Matdance 会先试 `/v1/messages`；如果提供商返回 resource-not-found 404，再试 `/messages`。成功路径会按 API 类型、Base URL 和模型 ID 缓存。千帆类 host 还会同时发送 Bearer 兼容鉴权头。

Anthropic-compatible 端点当前临时禁用 thinking 输出，和 OpenAI-compatible 侧的稳定性策略一致。

每个 agent 下面还有一组运行时目录。`runtime/browser_cookies/cookies.json` 保存浏览器 cookie，`runtime/events/` 和 `runtime/jobs/` 保存后台事件与作业状态；这些都属于本机敏感状态。文件预览接口现在只服务 `browser_temp/`、agent `workspace/` 和只读的内置提示音资源目录这类预览安全区域，不会把 `config/`、全局多模态配置、cookie store 或运行状态文件暴露给前端预览。

关键 JSON 状态写入采用“同目录临时文件 -> 替换目标文件”的方式，覆盖会话、agent 配置、定时任务、运行记录、向量索引和后台作业状态。调度任务启动时会先写 active run 和 `running` 运行记录，再进入实际执行；恢复逻辑能处理只写完其中一边就崩溃的情况。它不是数据库事务，但能避免很多半截文件、幽灵运行锁和崩溃后 JSON 读不出来的问题。

DeepSeek 可以直接选 `api_type=deepseek`。系统会自动填充 `https://api.deepseek.com`，并按内置模型预设补全上下文窗口和最大输出。当前内置模型包括：

- `deepseek-v4-flash`
- `deepseek-v4-pro`
- `deepseek-chat`
- `deepseek-reasoner`

DeepSeek 的托管默认值当前是 `context_window=1000000`、`max_output_token=384000`。通常你只需要填 agent 名称、API key、模型和温度；窗口和输出上限不要手欠乱改，除非你明确知道提供商现在改了规则。

Z.AI GLM 可以直接选 `api_type=zai_glm` 或 `api_type=zai_glm_coding_plan`。系统会自动填充 `https://api.z.ai/api/paas/v4`，并按内置模型预设补全上下文窗口和最大输出。`zai_glm` 的当前内置模型包括：

- `glm-5.1`
- `glm-5-turbo`
- `glm-4.7`
- `glm-4.5`
- `glm-4.5-air`
- `glm-4.5-x`
- `glm-4.5-airx`
- `glm-4.5-flash`

`zai_glm_coding_plan` 是面向编程/规划场景的精简 preset，包含 `glm-5.1`、`glm-5-turbo`、`glm-4.7`、`glm-4.5-air`。两者都支持 thinking 字段，API key 在 https://z.ai/manage-apikey/apikey-list 获取。

Baidu Qianfan Coding Plan 可以直接选 `api_type=baidu_qianfan_coding_plan`。系统会自动填充 `https://qianfan.baidubce.com/v2/coding`，并按内置模型预设补全上下文窗口、最大输入和最大输出。当前内置模型包括：

- `qianfan-code-latest`
- `deepseek-v3.2`
- `kimi-k2.5`
- `glm-5`
- `minimax-m2.5`
- `ernie-4.5-turbo-20260402`
- `deepseek-v4-flash`
- `glm-5.1`

`qianfan-code-latest` 是千帆控制台托管的固定模型名，实际底层模型由千帆侧配置决定；Matdance 对它使用保守默认窗口。想要更可解释的额度和行为时，建议直接选择明确的模型 ID。API key 在 https://console.bce.baidu.com/qianfan/resource/subscribe 获取。

Xiaomi MiMo 可以直接选 `api_type=xiaomi_mimo`。系统会自动填充 `https://api.xiaomimimo.com/v1`，并按内置模型预设补全上下文窗口和最大输出。当前内置模型包括：

- `mimo-v2.5-pro`
- `mimo-v2.5`
- `mimo-v2-pro`
- `mimo-v2-omni`
- `mimo-v2-flash`

MiMo 的托管默认值当前是 `context_window=1000000`（`mimo-v2-omni` 和 `mimo-v2-flash` 为 `256000`）。API key 在 https://platform.xiaomimimo.com/#/console/api-keys 获取。

目前版本还没有做专门的缓存命中优化，所以，我强烈建议搭配coding plan类的套餐来使用此系统，比如Z.ai GLM/Qwen coding plan这类coding plan套餐 或者 Xiaomi mimo token plan之类的token套餐服务。这类服务商通常提供更划算的大模型服务。由于coding plan套餐太过于火爆导致十分难抢，所以有些时候考虑中转服务也不错，我们提供了Openai和anthropic兼容的端点，你可以自行搭配，但要确保服务内容属实，中转站这类灰色地带，我们不为用户评估其可用性和真实性，属于风险与收益并存，而风险大于收益的范畴。

Kimi coding plan 对 agent 平台支持有限，所以不建议通过伪装成受支持 agent 平台的方式强行使用 Kimi coding plan 服务。请严格遵守其相关规定，避免造成个人账号资产方面的损失。当前如果需要使用 Kimi 类模型，应优先走明确支持 OpenAI-compatible 调用的合规端点，例如千帆 Coding Plan 暴露的 `kimi-k2.5`或 Kimi 自家开放平台的 API 调用服务。

### 模型选择建议

Matdance 对模型的要求不只是“会聊天”。工具调用、结构化输出、长上下文理解、错误恢复、按步骤执行和不要自我循环，都会直接影响这套系统能不能长期稳定使用。

我强烈建议优先使用 GLM 系列。它通常能在日常聊天、复杂任务、工具调用和长流程执行之间找到比较稳的平衡点，也就是更通用。其次推荐 DeepSeek 系列，尤其是需要较强推理或较长上下文时。

Kimi 类模型可以用，但要特别留意过度思考风险。Kimi K2 thinking 协议允许同一条 assistant 消息同时带 `reasoning_content` 和真实 `tool_calls`，这不是伪造工具请求，Matdance 会按协议保存 reasoning 并执行真实工具调用；Kimi thinking 请求会按官方约束使用 `temperature=1.0`，显式关闭 thinking 的后台结构化流程则使用非 thinking 固定温度。需要拦截的是 thinking 文本里手写的伪工具 JSON、`{play_audio:...}`、`{show_file:...}` 或重复复读。部分 Kimi 模型仍可能表现为无限思考、复读、反复执行相似步骤；如果你的任务里没有遇到这种过度思考问题，Kimi 也可以是不错的选择，一旦出现循环，优先换模型或降低任务复杂度。

复杂任务通常不建议使用 Minimax-M 系列作为主力。它更适合日常聊天、摘要、报告和情绪陪伴类任务；面对编码、复杂推理、复杂步骤执行和长期自动化维护时，它产出的情绪价值往往大于实用价值。把它用在报告型任务上没问题，把它当成复杂工程 agent 的主心骨就容易翻车。

`agents/<agent>/config/identity.md` 是 agent 的身份设定。这里写的是“这个 agent 是谁、性格如何、擅长什么、该如何说话”。

`agents/<agent>/config/user.md` 是 agent 对用户的理解。这里更适合放稳定信息，比如用户偏好、长期习惯、沟通风格，而不是每天都会变的流水账。

## Thinking 模型

Matdance 的协议层能解析多种 reasoning 字段，但当前为了稳定性临时禁用 thinking 输出。OpenAI-compatible 和 Anthropic-compatible 请求路径不会主动开启 thinking，也不会保存或展示新返回的 reasoning/thinking 内容。

Web Chat 仍保留 thinking 卡片能力；历史消息如果已经保存了 `reasoningContent`，重新打开会话时仍会恢复这张 thinking 卡片。

默认策略当前是：优先保证工具协议、JSON 稳定性和兼容提供商行为一致，不在普通对话或工具结果整合轮次启用 thinking。工具定义仍会随请求发送，模型仍能决定是否调用工具；只是 reasoning/thinking 字段暂时不参与请求和展示。

## 多模态配置

多模态配置保存在：

```text
agents/multimodal_config.json
```

它底层分两层：`global` 是全局默认值，`agents.<agent-name>` 是单个 agent 的覆盖值。当前 Settings 页面只编辑全局配置，agent 覆盖值保留兼容，但不再摆到界面里添乱。API key 只写入不回显；输入框留空就是保留旧 key，不是把 key 清掉。这点很重要，别让一个空输入框成为配置谋杀案。

图像生成和 TTS 都不是只绑一个模型。Settings 里可以配置多个 image profile 和多个 TTS profile，每个 profile 都有自己的名称、模型、base URL、voice/尺寸、输出格式和 API key。图像生成固定走 `/images/generations`；TTS 根据 profile 的 endpoint 模式走 `/audio/speech`、`/tts`、DashScope 或 `/chat/completions`。

agent 不确定有哪些图像提供商时，应该先调用 `image_generation_list_profiles`。如果用户没有指定提供商，通常直接调用 `image_generation` 并省略 `profile`，让系统走默认/自动 profile 顺序；如果用户明确说了 profile 名称，再把它传给 `profile`。图像生成在主会话里是宿主级异步任务，工具会返回 `job_id` 和 `batch_id`；相关图片复用同一个 `batch_id`，状态和文件位置以宿主通知或 `image_generation_show_process` 为准。普通后台定时任务 subagent 会同步等待图像生成结果，避免任务半途结束。用户改需求或连续失败像余额/鉴权/模型/服务问题时，先用 `image_generation_cancel` 停止 queued/running 任务。图像 prompt 默认保持 1-30 个字符，复杂场景最多 31-50 个字符。

agent 不确定有哪些语音提供商或 voice 时，应该先调用 `text_to_speech_list_profiles`。`text_to_speech` 通常不该在普通聊天里主动使用；它适合用户明确要求生成某句话、台词、稿子、旁白，也适合剪辑视频、创作项目等合理需要语音资产的场景。不传 `profile` 时走默认/自动 TTS profile 顺序；传 `texts` 可以一次生成多条音频资产。

Settings 还提供 Agent 提示音。默认提示音是短促的非人声系统音；你可以按语义分组浏览提示音类型，预览任意列表项，给每个类型批量上传或移除自定义音频，也可以启用/禁用某个类型或某个具体音频。默认音频同样能单独禁用；但如果主类型启用，至少要保留一个可播放音频。提示音库顶部是横向分类标签，下方按两列卡片排布；默认分类不支持新增类型，只有“自定义”分类可以新增、编辑、删除情绪类型，例如“开怀大笑”“鄙夷”“悠闲”。导入/导出按钮可以把自定义提示音配置和可嵌入音频一起分享。每个类型都有自己的音频列表，触发时只从启用项里随机播放。UI 会自动处理 thinking、回复完成和新定时任务通知；模型也可以在最终回复中使用 `{play_audio:TYPE}` 主动触发，支持 `reply_done`、`thinking`、`confused`、`help`、`confident`、`low_confidence`、`idea`、`happy`、`sad`、`perfunctory`、`considering`、`working_hard`、`tired`、`energized`、`angry`、`relieved`、`awkward`、`surprised`、`apologetic`、`skeptical`、`alert`、`celebrate`、`gentle`、`playful`，也支持 Settings 中定义的自定义情绪名称。这些 marker 会按出现顺序逐个解析成保留在对话里的状态卡；当前提示音开始播放后，marker 后续文本会进入该状态卡的内容区。遇到下一个提示音 marker 时，上一张卡先按 Settings 里的延迟设置收束，再开启下一张卡。聊天页不会因为新消息、回复完成或语音生成准备完成而强制滚到底部；离底部较远时会显示回底按钮。

提示音导出是 zip 资源包，里面有 `manifest.json` 和 `audio/<type>/...` 音频文件；本机 `/api/file?...` URL 只用于当前机器播放，不会作为跨用户传播内容写进包里。导入到另一个 agent 时，包内音频会先落盘到 `workspace/generated/audio/cues/<type>/`，再写入新的本地可播放路径，所以不会只同步名称和启停参数。导入带音频的包前需要先选中目标 agent；旧版 JSON/data URL 包仍能作为兼容格式导入。精确的 `{play_audio:TYPE}` 只在最终回复中触发播放和状态卡；出现在 thinking、`/think` 或 reasoning 中时只作为推理文本保存，不会被当作控制标记处理。需要讨论这个系统时，用普通文字描述即可。

目前支持这些多模态路径：

- `image_generation`：异步调用 `/images/generations`，生成文件默认保存到 `agents/<agent>/workspace/generated/images/`，并通过 `image_generation_show_process` 查询权威状态、fallback、错误和输出位置。
- `text_to_speech`：默认调用 `/audio/speech`，也可以走 `/tts`、阿里云 DashScope 或 `/chat/completions`，生成音频默认保存到 `agents/<agent>/workspace/generated/audio/`。它既支持 Chat/Lab 的消息 TTS，也支持 agent 作为工具生成语音资产。Chat 可见回复会在最终 `done` 事件后立即准备 TTS，不等所有提示音状态卡延迟结束；如果开启自动播放，会等最后一个提示音或回复完成提示音结束后再播放。
- `speech_to_text`：聊天页和 Lab 里的录音识别走浏览器 Web Speech 路径，通常会调用浏览器背后的在线识别服务；它轻量，也不保证每个浏览器都支持，尤其不要指望每个 macOS 浏览器表现一致。

TTS 的 `Endpoint` 模式：

- `native endpoint`：按 OpenAI 原生 TTS 路径走 `/audio/speech`。
- `v1/tts`：给那些把语音合成挂在 `/tts` 的提供商用。
- `Aliyun Qwen TTS`：走阿里云百炼 DashScope 千问 TTS 路径。Settings 里不需要填 Base URL，默认使用 `https://dashscope.aliyuncs.com/api/v1`。
- `v1/chat/completions`：仅用于 TTS 中转兼容，会尝试读取 chat audio payload。

TTS 模式：

- `off`：不生成语音。
- `chat_visible_only`：只有用户实际在 Chat 页面看到最终回复时，前端才会补一份语音。
- `always`：后端在最终回复落盘时就生成语音。它更主动，也更花钱。

Lab 页面可以直接测试图像生成、语音合成和语音转文字。图像和 TTS 按钮跟 agent/tool/chat 用的是同一套后端路径，所以 Lab 里失败就是真的失败，不是 UI 没给模型面子；STT 当前走浏览器 Web Speech 录音识别，浏览器不支持时就别硬掰。

## 构建检查

Windows PowerShell：

```powershell
dotnet build src\Matdance.Cli\Matdance.Cli.csproj -c Release --no-restore
```

macOS / Linux：

```bash
dotnet build src/Matdance.Cli/Matdance.Cli.csproj -c Release --no-restore
```

如果你正在处理很早以前启动的旧 Web UI，或者怀疑守护任务仍在用旧输出目录，可以先用：

```bash
matdance stop-all
```

它会同时关闭 Web UI 并禁用 hook/keep/boot，避免旧守护任务又把 Web UI 拉起来。正常情况下，新版托管 Web UI 会从 `.matdance/web-ui-shadow/` 启动，源码包装入口也会在 `dotnet run` 前自动暂停/恢复服务，所以不应该再因为 `Matdance.Plugins.Browser.dll` 被后台进程占用而构建失败。构建完成后再按需要重新执行 `matdance web-ui start --mode keep-alive-no-autostart --port 8765`。

