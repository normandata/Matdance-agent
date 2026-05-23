<p align="center">
  <img src="src/Matdance.Cli/Web/Assets/Brand/matdance-logo.png" alt="Matdance" width="260">
</p>

# Matdance

Language: [English](README.md) | 中文

当前版本：v1.1.21-preview

Matdance 是一个本地优先的 C# Agent 运行时。它把 Web UI、会话、记忆、技能、工作区、定时任务、浏览器自动化、文件预览、多模态资产生成和后台维护任务放进同一套本地持久化系统里。

它不是普通聊天壳。Matdance 更关心的是 agent 能不能保留状态、整理经验、复用技能、补偿后台任务，并把关键过程落到用户能检查的本地文件里。

强烈建议阅读 [FULL-DOC.zh-CN.md](FULL-DOC.zh-CN.md)。README 只负责仓库入口和索引；更多解释、边界、代价和设计动机都在完整文档里。`FULL-DOC.md` 与 `FULL-DOC.zh-CN.md` 应保持同等完整内容，只是语言不同。

## Preview 状态

- `v1.1.21-preview` 优化主会话上下文压缩。自动压缩和手动 `/compact` 现在按完整请求预算估算，保护最近 3 轮用户上下文，使用可降级/可回升的分段压缩，只在压缩重试那一轮注入一次性 handoff，并在后续回合复用 sidecar 摘要。
- `v1.1.21-preview` 同时加固了定时任务。Agent 创建任务时会更严格确认时区、规则、内容和投递目标；支持专属只读通知会话；用户级任务可执行真实手动测试但不推进原计划；主 agent 触发的测试通过 `scheduled_task_do_a_test` 入队，避免与当前回合争抢并发预算；定时任务通知卡片已修复为完整渲染 subagent 的 Markdown 输出，不再被正文内部的分隔线截断。
- `v1.1.21-preview` 改进技能学习、验证和后台整理。外部技能导入会保留具体资源文件，学习阶段按文件批次自适应降级/回升，导入验证不会覆盖原始导入资源；普通验证仍可通过更严格的质量门控修复资源；后台技能整理继续使用按轮次的 `skill_read`、降级/回升和毒性批次 skip 来保持自修复能力。
- `v1.1.21-preview` 在图像生成之外新增图片编辑支持。Debug Lab 现在提供生成/编辑切换，宿主支持 OpenAI 兼容的 `/images/edits` 路径，agent 可通过新的 `image_edit` 工具传入一张本地源图和简短编辑提示词。主 agent 调用保持异步，定时任务/subagent 可按宿主策略同步执行。
- 已在 `v1.1.20-hot-fix-2` 修复：技能整理可能因模型上下文超限，或因同一段异常证据反复失败而阻塞后续会话继续沉淀技能。现在技能整理使用可回升的自适应批次、按轮次工作的 `skill_read` 窗口、只保留 tool call 的证据压缩，以及可恢复失败超过阈值后的毒性批次 skip。

## 目录

- [界面预览](#界面预览)
- [快速开始](#快速开始)
- [系统能力](#系统能力)
- [文档索引](#文档索引)
- [运行边界](#运行边界)
- [许可与声明](#许可与声明)

## 界面预览

| Home | Chat |
| --- | --- |
| <img src="docs/assets/ui/Main.png" alt="Matdance Home" width="420"> | <img src="docs/assets/ui/Chat.png" alt="Matdance Chat" width="420"> |

| Agent | Memory |
| --- | --- |
| <img src="docs/assets/ui/Agents.png" alt="Matdance Agent" width="420"> | <img src="docs/assets/ui/Memorys.png" alt="Matdance Memory" width="420"> |

| Skills | Lab |
| --- | --- |
| <img src="docs/assets/ui/Skills.png" alt="Matdance Skills" width="420"> | <img src="docs/assets/ui/Debug-Lab.png" alt="Matdance Lab" width="420"> |

| Settings | Settings Details |
| --- | --- |
| <img src="docs/assets/ui/Settings_1.png" alt="Matdance Settings" width="420"> | <img src="docs/assets/ui/Settings_3.png" alt="Matdance Settings Details" width="420"> |

## 快速开始

Matdance 需要 .NET 9 SDK。源码启动和构建不依赖 Java、npm 或用户手动安装 Node.js；浏览器自动化使用 Playwright 自带的运行时和 Chromium 依赖。

Windows PowerShell：

```powershell
dotnet restore src\Matdance.Cli\Matdance.Cli.csproj
.\matdance.ps1
```

macOS / Linux：

```bash
dotnet restore src/Matdance.Cli/Matdance.Cli.csproj
chmod +x ./matdance
./matdance
```

首次使用浏览器自动化前，在菜单里安装依赖，或执行：

```bash
./matdance deps install --source global
```

Windows 对应：

```powershell
.\matdance.ps1 deps install --source global
```

更完整的启动、入口注册、Web UI 托管、模型配置和多模态配置见 [quickly_start.zh-CN.md](quickly_start.zh-CN.md)。

## 系统能力

- Web UI 优先：Chat、Agent、Schedule、Skills、Memory、Lab、Settings 都在浏览器界面内完成。
- 多 agent 本地管理：每个 agent 有独立配置、人设、用户画像、会话、记忆、技能、定时任务和工作区。
- 分层记忆：hot/core/long-term/vector memory 分别处理近期状态、稳定事实、日期档案和本地检索。
- 技能系统：支持编辑、整理、导出 zip、学习并验证外部材料、闲时验证和受控修复。
- 定时任务系统：支持一次性、每日、多次、窗口循环任务，并在重启、休眠或中断后补偿错过触发。
- 浏览器自动化：基于 Playwright 的受控 Chromium，支持导航、点击、输入、截图、读取页面和 cookie 保存/应用诊断。
- 文件附件与预览：Chat 支持最多 3 个附件，内联 `{show_file:...}` 可展示图片、HTML、Markdown、文本、音频和常见文档。
- 多模态工具：支持宿主级异步图像生成与图片编辑、TTS 资产生成、Chat/Lab 语音播放，以及浏览器 Web Speech 录音识别。
- 本地可检查：会话、记忆、技能、任务运行记录和工作区文件都落在本地目录里。

## 文档索引

- [FULL-DOC.zh-CN.md](FULL-DOC.zh-CN.md)：完整系统解释，建议先读。
- [quickly_start.zh-CN.md](quickly_start.zh-CN.md)：启动、依赖、入口注册、模型配置和常用 CLI。
- [docs/zh-CN/system-overview.md](docs/zh-CN/system-overview.md)：系统在做什么。
- [docs/zh-CN/system-boundaries.md](docs/zh-CN/system-boundaries.md)：系统边界、代价和不承诺事项。
- [docs/zh-CN/web-ui-and-security.md](docs/zh-CN/web-ui-and-security.md)：Web UI、远程访问和安全边界。
- [docs/zh-CN/data-layout.md](docs/zh-CN/data-layout.md)：本地数据与目录结构。
- [docs/zh-CN/memory.md](docs/zh-CN/memory.md)：记忆系统。
- [docs/zh-CN/skills.md](docs/zh-CN/skills.md)：技能系统。
- [docs/zh-CN/scheduled-tasks.md](docs/zh-CN/scheduled-tasks.md)：定时任务与后台可靠性。
- [docs/zh-CN/tools-and-multimodal.md](docs/zh-CN/tools-and-multimodal.md)：工具、浏览器与多模态。
- [docs/zh-CN/runtime-and-development.md](docs/zh-CN/runtime-and-development.md)：运行守护与开发说明。

## 运行边界

Matdance 是本地优先系统，不是云端多用户平台。默认 Web UI 只绑定本机地址；远程绑定需要显式开启，并使用单 token 鉴权。

隐私访问开关是实时权限信号。理想情况下，agent 会在开关关闭时拒绝读取桌面、照片、私有文档、社交平台、邮箱、私信、论坛账号页等隐私内容。但 prompt、工具描述和 host 侧拦截不能等同于绝对保险柜。高价值隐私数据仍应由用户自行筛选、脱敏，再交给 agent。

Matdance 源码、插件源码、`.matdance/state`、Web 鉴权状态、supervisor 状态、shadow 运行目录、运行队列、任务运行记录、cookie store、agent 配置、模型凭据、API key、token、密码和授权文件属于系统稳定性边界。agent 不应作为媒介修改这些内容。

## 许可与声明

本仓库使用 [MIT-0](LICENSE) 许可发布。

使用前请阅读 [DISCLAIMER.md](DISCLAIMER.md)。如果你要远程暴露 Web UI、处理隐私数据、连接第三方模型或运行浏览器自动化，也请阅读 [SECURITY.md](SECURITY.md)。


