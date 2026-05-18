# 工具、浏览器与多模态

Language: [English](../tools-and-multimodal.md) | 中文

Matdance 的工具系统让 agent 可以执行受控文件操作、终端命令、记忆写入、任务管理、技能管理、浏览器自动化和多模态资产生成。

每次工具调用都有宿主侧执行超时。超时结果是权威信号，表示请求范围过大、目标卡住或依赖无响应；agent 不应原样重复调用，而应缩小文件/页面/查询/任务范围、关闭异常锁，或请求用户介入。

## 本地执行

`bash` 工具在 agent workspace 下执行命令，并有超时限制。Windows 下实际通过当前运行时配置的 shell 调用，macOS/Linux 使用对应 shell。系统 prompt 会注入当前 OS、架构、shell 和路径风格，帮助模型选择正确命令。

Web UI 模式不会弹出交互确认窗口。危险命令会被拒绝或要求在 CLI/人工维护边界外处理。

## 文件工具

`file_read` 和 `file_write` 主要面向 agent workspace 和预览安全的运行输出。Matdance 源码、插件源码、`.matdance/state`、运行队列、任务运行记录、cookie store、agent config、凭据和授权文件不应通过 agent 文件工具访问或修改。

文件工具现在围绕实时窗口工作：

- `file_search`：只用于导航搜索，返回结果不是稳定编辑坐标。搜索受文件数量、文件大小、重目录跳过和短时间预算限制，避免大范围搜索卡死 agent 回合。
- `file_trace_open`：打开本轮回复内有效的实时 Read 锁，可按文件、行范围或 anchor 建立窗口。最多 3 把 Read 锁，每把最多 2000 行；元数据读取、正文读取和锁内容渲染都有边界。semantic 锁会尝试跟随移动后的代码块，physical 锁显示固定物理行范围。
- `file_trace_show`：从磁盘刷新 Read/Write 锁。当前锁内容比旧片段、记忆里的行号和用户声称都更权威。
- `file_trace_close`：关闭不再需要的锁。
- `file_write`：写入、追加、覆盖或按 `expected` + `replace_with` 做精确替换。每次成功写入都会自动打开或刷新修改区域附近的 Write 锁。
- `file_write_locks` / `file_write_lock_close`：查看或关闭已验证完的 Write 锁。

Write 锁是自动验证窗口，覆盖修改中心附近上下约 100 行，并根据文件头尾自适应。最多保留 3 把；如果第 4 次远距离写入需要新锁，系统会先拒绝写入，要求 agent 显式关闭一把已确认无误的 Write 锁。

Read/Write 锁会在本轮回复结束时自动清空，下一轮开始也会丢弃旧锁。完整文件编辑 diff 会写入会话状态做审计。给模型的协议重点是本轮实时锁内容，而不是长期复用的旧坐标。

当文件对用户有价值时，agent 应在可见回复中使用 `{show_file:PATH}` 展示预览。

## 重连重试策略

模型/API 重连使用批次探测，不再线性增加等待时间。每次重试探测固定等待 3 秒。批次大小逐级翻倍：第 1 批 10 次，第 2 批 20 次，第 3 批 40 次，以此类推，最多 10 批。主聊天、定时任务 subagent、记忆/技能维护 subagent 和多模态 HTTP 调用，对可重试的网络错误、超时、429、5xx 失败使用同一策略。

## 浏览器自动化

浏览器工具基于一套预热 Chromium。它是全局单例，不按 agent 或 session 隔离；操作通过锁串行执行。Web UI 的浏览器浮层可以观察实时画面。

浏览器以后台优先方式运行。`headless:false` 这类请求会被忽略，系统不会把原生浏览器窗口拉到前台干扰用户。Web UI 浮层是观察和用户登录的主要表面。

常用边界：

- 登录、验证码、CAPTCHA、账号选择等步骤应交给用户在可用的用户可控认证表面完成。
- agent 不应关闭登录弹窗、绕过认证、猜测凭据或替用户输入密码/验证码。
- 不要把刷新、切页、重开浏览器或 `browser_close` 当成通用恢复手段。系统会尽量维护同一个浏览器/context 的存活状态，保留当前页面和登录态。
- `browser_close` 在普通 agent 调用下是兼容 no-op。浏览器会在 Web UI 关闭或宿主释放时统一清理。
- `browser_evaluate` 只适合短 JavaScript：同步 DOM 读取、轻量点击/赋值、快速返回的状态检查。它有超时边界，不应放入无限轮询、等待登录、等待网络、长 promise、计时器或前台窗口控制。
- 浏览器启动、页面创建、全局操作锁、导航、点击/输入、等待、验证、截图、正文/源码读取、滚动、爬取、追踪、注入和 cookie 操作都有宿主侧超时边界。`wait_network_idle` 最多 30 秒，click/type/wait/verify 的 timeout 最多 30 秒，scroll 总预算 45 秒，crawl 总预算 90 秒。超时会作为工具结果返回，让 agent 换更小的步骤或请求用户介入，而不是长期占住浏览器队列。

动态页面应优先使用有边界的专用工具，而不是在 `browser_evaluate` 里写长轮询：

- `browser_wait_for`：等待 selector、页面文本、URL 条件、load state，或短小安全的 predicate。
- `browser_query`：返回结构化 DOM 候选项，包括文本、角色、标签、链接和 selector 建议，适合点击前定位元素。
- `browser_source_analyze`：返回页面源码结构清单，包括脚本、样式、表单、metadata、链接和内联事件处理器位置，但不读取 browser storage 或凭据值。
- `browser_verify`：在跳转、点击、输入、注入或爬取后确认 selector/text/URL/load-state/predicate 条件。
- `browser_crawl`：按有限页面数和深度跟随链接，默认同源，返回标题、正文摘要、链接摘要，并遮蔽敏感 URL query 值。
- `browser_trace`：启动、读取或停止高层网络/console 追踪；不会记录请求/响应头、body、cookie、storage、凭据或 token 明值。
- `browser_scroll`：按有限步数滚动页面或容器，可在出现指定 selector/text 后停止。
- `browser_inject_init_script`：为后续导航注入最多 25000 字符的初始化脚本；脚本中出现 cookie、storage、凭据、token、验证码、paywall、特权请求头、service worker 或反爬指纹绕过相关内容会被拒绝。

这些工具只用于普通动态加载、懒加载和前端渲染等待。它们不是认证、验证码、付费墙、反爬或站点规则的绕过层。

## Cookie 工具

`save_cookie`、`list_cookie_by_site`、`apply_cookie` 用于受控浏览器登录态复用。它们默认全量保存或应用当前 agent 的浏览器 cookie，也可按主域过滤。

这些工具不会返回 cookie 明值。cookie 值只应通过受控保存/应用流程在浏览器内部使用，不能展示、复制、导出或传给其它工具。隐私访问开关不禁用这些受控 cookie 操作，但关闭隐私访问时，恢复登录态后仍不能读取或导出用户私有账号内容。

`apply_cookie` 成功只表示 cookie 已写入受控浏览器 context，不等于当前页面立刻变成已登录状态。已经加载的页面可能需要站点自身的正常导航或重载才会读取新 cookie；当前页面如果不在目标站点范围内，也需要先导航到对应站点。遇到登录墙时，正确做法是让用户在 Web UI 浮层里完成登录，再保存 cookie，而不是反复关闭、刷新或绕过登录界面。

## 图像生成

`image_generation` 会启动宿主级异步图像生成任务，底层调用配置的 `/images/generations` 或兼容端点，默认输出到：

```text
agents/<agent>/workspace/generated/images/
```

如果用户没有指定 provider/profile，通常省略 `profile`，让系统按默认顺序选择。需要确认可用配置时，先调用 `image_generation_list_profiles`。

图像任务不会阻塞 agent 回合。工具会返回 `job_id` 和 `batch_id`；相关图片应复用同一个 `batch_id`。状态、失败原因、provider fallback、最终 provider/model、prompt 到文件的映射和文件位置，只以宿主通知或 `image_generation_show_process` 为准。用户声称“好像生成了”或“是不是失败了”只能作为反馈，不能当成事实。任务完成时，如果会话正在回复，宿主会把通知插入当前回合；如果会话空闲，前端会用消息级通知触发一轮 continuation，让主 agent 及时处理结果。用户改需求或连续失败表现为余额、鉴权、模型不可用、服务故障时，应先用 `image_generation_cancel` 停止 queued/running 任务，再创建替代任务。已成功生成的文件默认保留。

普通后台定时任务 subagent 是例外：它会同步执行图像生成工具，直接拿到最终图片或失败原因，避免任务在图片还没回来时半途结束。

图像 prompt 默认应保持 1-30 个字符。只有用户明确要求复杂画面，或确实无法压缩而不丢失需求时，才允许 31-50 个字符。

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

