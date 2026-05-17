# 工具、浏览器与多模态

Language: [English](../tools-and-multimodal.md) | 中文

Matdance 的工具系统让 agent 可以执行受控文件操作、终端命令、记忆写入、任务管理、技能管理、浏览器自动化和多模态资产生成。

## 本地执行

`bash` 工具在 agent workspace 下执行命令，并有超时限制。Windows 下实际通过当前运行时配置的 shell 调用，macOS/Linux 使用对应 shell。系统 prompt 会注入当前 OS、架构、shell 和路径风格，帮助模型选择正确命令。

Web UI 模式不会弹出交互确认窗口。危险命令会被拒绝或要求在 CLI/人工维护边界外处理。

## 文件工具

`file_read` 和 `file_write` 主要面向 agent workspace 和预览安全的运行输出。Matdance 源码、插件源码、`.matdance/state`、运行队列、任务运行记录、cookie store、agent config、凭据和授权文件不应通过 agent 文件工具访问或修改。

当文件对用户有价值时，agent 应在可见回复中使用 `{show_file:PATH}` 展示预览。

## 浏览器自动化

浏览器工具基于一套预热 Chromium。它是全局单例，不按 agent 或 session 隔离；操作通过锁串行执行。Web UI 的浏览器浮层可以观察实时画面。

浏览器以后台优先方式运行。`headless:false` 这类请求会被忽略，系统不会把原生浏览器窗口拉到前台干扰用户。Web UI 浮层是观察和用户登录的主要表面。

常用边界：

- 登录、验证码、CAPTCHA、账号选择等步骤应交给用户在可用的用户可控认证表面完成。
- agent 不应关闭登录弹窗、绕过认证、猜测凭据或替用户输入密码/验证码。
- 不要把刷新、切页、重开浏览器或 `browser_close` 当成通用恢复手段。系统会尽量维护同一个浏览器/context 的存活状态，保留当前页面和登录态。
- `browser_close` 在普通 agent 调用下是兼容 no-op。浏览器会在 Web UI 关闭或宿主释放时统一清理。
- `browser_evaluate` 只适合短 JavaScript：同步 DOM 读取、轻量点击/赋值、快速返回的状态检查。它有超时边界，不应放入无限轮询、等待登录、等待网络、长 promise、计时器或前台窗口控制。
- 导航、截图、正文读取、标题读取和全局操作锁都有超时边界。超时会作为工具结果返回，让 agent 换更小的步骤或请求用户介入，而不是长期占住浏览器队列。

动态页面应优先使用有边界的专用工具，而不是在 `browser_evaluate` 里写长轮询：

- `browser_wait_for`：等待 selector、页面文本、URL 条件、load state，或短小安全的 predicate。
- `browser_query`：返回结构化 DOM 候选项，包括文本、角色、标签、链接和 selector 建议，适合点击前定位元素。
- `browser_scroll`：按有限步数滚动页面或容器，可在出现指定 selector/text 后停止。
- `browser_inject_init_script`：为后续导航注入小型初始化脚本；脚本中出现 cookie、storage、凭据、token、验证码、paywall、网络拦截或反爬指纹绕过相关内容会被拒绝。

这些工具只用于普通动态加载、懒加载和前端渲染等待。它们不是认证、验证码、付费墙、反爬或站点规则的绕过层。

## Cookie 工具

`save_cookie`、`list_cookie_by_site`、`apply_cookie` 用于受控浏览器登录态复用。它们默认全量保存或应用当前 agent 的浏览器 cookie，也可按主域过滤。

这些工具不会返回 cookie 明值。cookie 值只应通过受控保存/应用流程在浏览器内部使用，不能展示、复制、导出或传给其它工具。隐私访问开关不禁用这些受控 cookie 操作，但关闭隐私访问时，恢复登录态后仍不能读取或导出用户私有账号内容。

`apply_cookie` 成功只表示 cookie 已写入受控浏览器 context，不等于当前页面立刻变成已登录状态。已经加载的页面可能需要站点自身的正常导航或重载才会读取新 cookie；当前页面如果不在目标站点范围内，也需要先导航到对应站点。遇到登录墙时，正确做法是让用户在 Web UI 浮层里完成登录，再保存 cookie，而不是反复关闭、刷新或绕过登录界面。

## 图像生成

`image_generation` 调用配置的 `/images/generations` 或兼容端点，默认输出到：

```text
agents/<agent>/workspace/generated/images/
```

如果用户没有指定 provider/profile，通常省略 `profile`，让系统按默认顺序选择。需要确认可用配置时，先调用 `image_generation_list_profiles`。

聊天附件里的图片走模型主 LLM 请求，不走 `image_generation`。Matdance 会先给未知模型一次携带图片 payload 的机会；如果上游明确拒绝图片/多模态输入，会立刻改为不带图片重试，并把该 provider/model 记录为 text-only。后续同模型默认只传文件名、路径和元数据，避免每次都撞一次多模态错误。

如果图片请求失败原因不明确，Matdance 会先不带图片快速重试；只有文本请求也失败时才进入普通 LLM retry。连续出现“带图失败、去图成功”的情况后，系统会把该 provider/model 暂时视为不支持视觉输入。这个判断是运行时能力缓存，不是 agent 记忆。

## TTS

TTS 有两类路径：

- Chat/Lab 的消息级语音生成与播放。
- agent 主动调用 `text_to_speech` 生成语音资产。

支持的 endpoint 模式包括 OpenAI 原生 `/audio/speech`、`/tts`、阿里云 DashScope 千问 TTS，以及部分 `/chat/completions` 兼容中转。

长文本应尽量按句子分批。当上游返回长度、payload 或超时类错误时，Matdance 可以把文本拆成最多 10 个以句号结尾的片段，并行重试后合成为一个最终音频。

如果还是失败，则在播放语音时通过一个UI层来告知用户具体的报错内容。方便调试。

## Web Search

`web_search` 走 Settings -> Multimodal 里的搜索 profile。默认预置三类提供商：

- Tavily：`https://api.tavily.com/search`
- Brave Search：`https://api.search.brave.com/res/v1/web/search`
- Firecrawl：`https://api.firecrawl.dev/v1/search`

这些预置默认禁用。用户需要启用 profile 并保存 API key；Key 在 UI 中只写不回显。agent 如果需要确认可用 provider，应先调用 `web_search_list_profiles`；普通搜索可省略 `profile`，让系统按已启用顺序自动选择并在失败时回退。

## STT

聊天页和 Lab 的录音识别走浏览器 Web Speech。它通常依赖浏览器背后的在线识别服务，具体可用性取决于浏览器和系统环境。

## Anthropic Messages 兼容端点

在 Matdance 里，`anthropic` 表示 Anthropic Messages 兼容协议，不等于只能使用 Anthropic 官方 URL。默认地址可以填官方 API，也可以换成兼容 Messages 协议的其他提供商地址；`model_id`、上下文窗口和最大输出也可以按兼容提供商的实际规则填写，不会被内置 Anthropic 预设锁死。

这条路径使用原生 Messages 结构：`system`、`tools` / `input_schema`、assistant 侧 `tool_use`、下一条 user 消息里的 `tool_result`、流式文本增量和工具参数增量。图片附件会按 Anthropic base64 image block 发送；如果上游模型或兼容端点拒绝多模态输入，Matdance 会降级为 text-only 并记录该 provider/model 的能力状态。

Anthropic-compatible 路径当前临时禁用 thinking 输出。这个稳定性开关开启时，Matdance 不会为该 API 类型请求、保存或展示 Anthropic `thinking` block。

`base_url` 可以填 API 根地址，也可以填兼容服务给出的完整 `/messages` 端点。对于 API 根地址，Matdance 会先试 `/v1/messages`；只有提供商返回 resource-not-found 404 时，才回退到 `/messages`。成功的文本端点会按 API 类型、Base URL 和模型 ID 缓存。千帆类 host 还会同时发送 Bearer 兼容鉴权头。

## 提示音

提示音不是 TTS。它是短促系统音，用于表达 agent 状态。内置类型包括 `reply_done`、`thinking`、`confused`、`help`、`confident`、`low_confidence`、`idea`、`happy`、`sad`、`perfunctory`、`considering`、`working_hard`、`tired`、`energized`、`angry`、`relieved`、`awkward`、`surprised`、`apologetic`、`skeptical`、`alert`、`celebrate`、`gentle` 和 `playful`。Settings 中新增的自定义类型也会同步到 prompt，供 agent 用 `{play_audio:TYPE}` 触发。

