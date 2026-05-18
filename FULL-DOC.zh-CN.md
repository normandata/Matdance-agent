# Matdance Full Documentation

Language: [English](FULL-DOC.md) | 中文

当前版本：v1.1.20-preview

这份文档是 Matdance 的完整解释文档。`README.md`、`quickly_start.md` 和 `docs/` 下的专题文档会尽量简洁，方便快速查入口、命令和局部机制；但如果你想真正理解这个系统在做什么、为什么这样设计、哪些边界不能乱碰，应该读这份 `FULL-DOC.zh-CN.md`。`FULL-DOC.md` 与 `FULL-DOC.zh-CN.md` 应保持同等完整内容，只是语言不同。

Matdance 不是“聊天窗口外面套一层皮”的转发壳，而是一个本地运行的 C# Agent 运行时。它把 Web UI、会话、记忆、技能、工作区、定时任务、浏览器自动化、文件预览、多模态资产生成和后台维护任务放进同一套本地持久化系统里。

它要解决的问题很直接：让 agent 在本地保留状态、整理经验、复用技能，并且让人能随时打开文件检查。所谓持续学习不是模型权重偷偷修炼，也不是在聊天框里给自己加戏；Matdance 积累的是 Markdown、JSON、索引、任务记录、运行报告和验证报告。能看、能改、能迁移，才有资格谈长期协作。

## v1.1.20-preview 摘要

v1.1.20-preview 延续前几个 preview 版本的边界和长期运行可靠性工作。

- README 被拆成简洁入口、专题文档和这份完整文档。README 不再承担所有解释。
- 系统内置记忆整理和技能整理任务改用英文稳定注册文案，Web UI 按当前语言显示系统任务标题和说明。
- 后台事件和子任务阶段文本统一改为英文运行数据，避免英文界面夹杂中文状态。
- 技能验证报告为 `needs_changes` 或 `invalid` 时继续进入待验证队列，让技能可以靠验证和修复循环继续成长。
- 技能支持导出为 zip 包；导入侧仍走“学习并验证”，外部技能包不会绕过安全本地化。
- 系统内置记忆整理和技能整理的 catch-up 按 `agent + taskId` 语义去重，停机期间错过多个触发点时只补偿一次整理；技能验证不参与启动补偿，继续走用户空闲时的状态驱动验证。
- `task_manager` 创建的任务最多保留 3 个步骤，避免长清单把 Chat UI 挤乱。
- Long-term memory 详情预览改为内部滚动面板，长档案不再把整个 Memory 页面撑成超长滚动。
- 隐私访问开关的实时性被写入系统 prompt 和工具描述。Settings 里的当前状态是唯一权限权威来源。
- 浏览器自动化改为后台优先：不拉起前台原生窗口，`browser_close` 在普通 agent 调用下是 no-op，系统维护共享 browser/context/page 的存活状态。
- 浏览器启动、页面创建、导航、动作、等待、验证、截图、正文/源码读取、滚动/爬取、追踪/注入、cookie 操作和浏览器全局操作锁都有超时边界，避免一个工具调用无限卡住队列。
- 每次工具调用都有宿主侧执行超时。超时结果是权威信号，agent 应缩小范围、关闭异常文件锁、减少页面/查询范围或请求用户介入，而不是原样重复调用。
- cookie 工具保持可用，但不返回明值；`apply_cookie` 会返回诊断，提醒 cookie 写入 context 不等于当前页面立刻已登录。
- 图片附件有 provider/model 级视觉能力缓存。未知模型会给一次带图机会，明确拒绝或多次带图失败后会默认按 text-only 处理，避免图片 payload 把普通 LLM retry 链拖很久。
- TTS 长文本失败时可按句子拆分，最多 10 段并行重试并合并最终音频；播放失败错误不再塞在聊天气泡下，而应以 UI 错误层提示。

## 当前能力

- 多 agent 本地管理：每个 agent 有独立配置、人设、用户画像、会话、记忆、技能、定时任务和工作区。
- Web UI 优先：日常聊天、配置、记忆、技能、定时任务、向量记忆图册、Lab 调试都通过 Web UI 完成。
- 主程序菜单：支持依赖下载、托管 Web UI、运行守护模式切换、状态查看、关闭 Web UI/hook/keep/boot，以及系统级 `matdance` 入口注册。
- 流式聊天与工具调用：模型回复、工具结果、任务步骤、文件预览和提示音状态会写回会话。
- thinking 模型兼容：系统能解析多种 reasoning 字段；当前在 OpenAI-compatible 和 Anthropic-compatible 请求路径上为稳定性临时禁用 thinking 输出，避免部分模型在 reasoning 段复读或嵌入异常工具请求。
- 跨会话记忆：hot memory、core memory、long-term memory 和 vector memory 分工保存。
- 本地向量数据库：不调用云端 embedding，使用本地特征哈希、SimHash、VP-tree 和 rerank 支撑模糊检索，并在 Memory 页面提供向量搜索与 2D 图册。
- 技能系统：支持手动编辑、整理、导出 zip、学习并验证外部材料、手动验证、闲时自动验证和有限自动修复。
- 定时任务系统：系统内置每 3 小时整理记忆、每 3 小时整理技能；服务器重启、休眠或中断后会补偿错过触发，用户任务按触发点补偿，系统整理任务按语义折叠补偿。
- 浏览器自动化：基于 Playwright 的受控 Chromium，支持导航、点击、输入、截图、读取页面、短 JS、cookie 保存/列出/应用。
- 文件附件：Chat 支持最多 3 个附件，类型包括常见图片、常见文档和 zip。图片会优先尝试作为模型多模态 payload。
- 内联文件预览：聊天消息支持 `{show_file:...}`，可以直接展示 HTML、图片、Markdown、代码、文本、音频和部分浏览器可打开的文档。
- 多模态端点：内置 `image_generation` 和 `text_to_speech` 资产生成工具，以及 Chat/Lab 的 TTS/STT 组件。
- 本地优先：agent 数据基本都在 `agents/` 里，能直接打开文件检查，不依赖云端黑盒。

## 启动和配置

启动、依赖安装、系统级 `matdance` 入口注册、agent 模型配置和多模态端点配置放在 [quickly_start.md](quickly_start.md)。

这里不重复粘命令。原因很简单：启动文档应该让系统跑起来，完整文档应该解释系统为什么是这样。把命令、参数、免责声明、设计哲学和所有子系统细节塞在一个 FULL-DOC 里，会很有语言张力，但检索性会下降。

当前推荐路径是：

- 安装 .NET 9 SDK。
- 首次使用前安装 Playwright Chromium 依赖。
- 通过源码包装脚本或注册后的 `matdance` 启动 Web UI。
- 日常聊天、配置、记忆、技能、定时任务、Lab 调试都走 Web UI。

Matdance 没有 Java 或 npm 项目依赖。Playwright 自带 Node runtime，用于安装和驱动浏览器，这不是要求用户安装 Node/npm，也不是 Java 依赖。

## Web UI

Web UI 是 Matdance 当前真正推荐的主入口。它承载了聊天、配置、记忆、技能、定时任务、向量记忆搜索和图册等完整工作流；CLI 是启动器、维修口和低层命令集合，不是完整日常交互界面。

默认情况下 Web UI 只应该绑定本机 loopback 地址。非 loopback host 会被拒绝，除非显式设置 `MATDANCE_ALLOW_REMOTE_WEB=1`。远程绑定会启用单 token 鉴权：同一时间最多只有一个 token 生效，浏览器登录通过 HttpOnly cookie 保存认证态，系统 API 也接受 `Authorization: Bearer <token>` 或 `X-Matdance-Token`。

Web UI 主要界面：

- Home：星球入口页，负责在各界面之间跳转。
- Chat：主 agent 对话页，支持流式消息、工具调用展示、任务进度、内联文件预览、浏览器画面浮层、附件、语音输入和回复语音播放。
- Agent：agent 配置页，可创建/删除 agent，切换模型、base URL、api type、上下文窗口、并发预算、头像等。
- Schedule：定时任务管理页，可创建一次性、每日、多次、窗口循环等任务，并查看运行历史。
- Skills：技能库管理页，可新建、编辑、删除、导出、整理、验证技能，也可以学习并验证外部技能材料。
- Memory：记忆管理页，可查看和编辑 core/hot/user/identity，可查看长期记忆，也可搜索本地向量库并查看图册。
- Lab：调试图像生成、语音合成和语音转文字的地方，用来拆穿配置幻觉。
- Settings：语言、本地偏好、隐私访问、记忆限制、后台事件、提示音和多模态端点配置。

## 本地数据布局

`agents/` 是所有智能体保留数据的根目录。一个 agent 的大部分状态都能通过名字定位到。

典型结构：

```text
agents/
  multimodal_config.json
  <agent-name>/
    config/
      agent_config.json
      identity.md
      user.md
    icons/
    sessions/
      <session-id>.json
      <session-id>.state.json
    memory/
      hot_memory/
        MEMORY.md
      core_memory/
        core_memory.md
      long_term_memory/
        YYYY-MM-DD.md
      vector_memory/
        base.json
    skills/
      <skill-id>/
        skill.md
        skill.json
        validation-report.md
        import-report.md
        scripts/
        assets/
        templates/
        resources/
        examples/
        config/
    scheduled_tasks/
      tasks.json
      runs/
        <task-id>/
          <run-id>.json
    runtime/
      browser_cookies/
        cookies.json
      events/
      jobs/
    workspace/
      attachments/
      generated/
        images/
        audio/
```

`agent_config.json` 可能包含 API key，`runtime/browser_cookies/cookies.json` 可能包含可复用登录态，`sessions/`、`workspace/`、`scheduled_tasks/runs/`、`runtime/events/` 和 `runtime/jobs/` 都属于本地运行痕迹，默认不应该提交。

`.matdance/` 是本机运行目录，不属于源码资产。它会放依赖、Playwright 浏览器、Playwright 驱动缓存、Web UI 状态、Web 鉴权 token、用户时区状态、注册入口脚本、Web UI shadow 运行目录和模型能力缓存。

典型结构：

```text
.matdance/
  deps/
    playwright-browsers/
    playwright-driver/
  state/
    web-ui.json
    web-auth.json
    user-time-zone.json
    model-capabilities.json
  web-ui-shadow/
  bin/
  logs/
```

关键 JSON 写入会先落到同目录临时文件，再替换目标文件。它不是数据库事务，但能降低半截 JSON、幽灵运行锁和崩溃后读不出来的概率。

## Sessions

`sessions/` 保存会话数据。每个 `<session-id>.json` 用于统计会话，`<session-id>.state.json` 保存消息、工具调用、当前任务和 UI 状态。

常见字段：

- `session_id`：会话 ID。
- `context_usage`：当前会话 prompt tokens 相对该 agent `context_window` 的使用比例。
- `total_messages`：源于该会话的用户消息总数。
- `tool_messages_count`：工具结果消息数量。
- `tokens`：本地估算的上下文 token 总量。
- `create_at`：会话创建时间。
- `last_activity`：最近活动时间，用于判断 agent 是否空闲。
- `is_processing`：该会话是否正在处理主 agent 回合。
- `tasks[]`：会话中拆出来的任务。

会话时间会按 Matdance 用户时区写入 JSON，并带 offset。Web UI 会用浏览器上报的时区更新 `.matdance/state/user-time-zone.json`，也可以用 `MATDANCE_TIME_ZONE` 固定；session id、run id、调度去重、cookie 过期、token 过期和进程超时仍使用 UTC/Unix 时间。

## Memory

记忆是跨会话保存的，这是 Matdance 和普通聊天壳最不一样的地方之一。它不是完整聊天记录的粗暴堆叠，而是把不同稳定程度的信息放在不同层级里。

### Hot Memory

路径：

```text
agents/<agent>/memory/hot_memory/MEMORY.md
```

Hot memory 是近期重要上下文，会注入 prompt。它适合保存最近发生过、短期内仍然很重要的信息：当前任务进展、刚形成的约定、最近问题状态、接下来几天还会继续用到的线索。

这里的关键变化是：hot memory 被视为“近期工作集”，不是长期档案。普通对话中的 `memory_store` 仍然是追加式记录，避免主 agent 在一轮对话里误删旧内容；但后台记忆整理任务会完整读取职责范围内的 hot/core/user/identity/long-term 文件，并返回完整覆盖 payload。整理任务有资格把已经归档、可检索、低时效的旧 hot memory 移出工作集，而不是把它们永久挤在 prompt 里。

因此，空间管理不再只是“把久远记录越压越薄”。近期且仍有操作价值的上下文拥有更高优先级：当前任务、未解决问题、刚形成的约定、短期承诺和接下来还要用的线索，不应该被压成没有操作价值的一句话。容量紧张时，系统优先移出已经进入 long-term memory 的久远内容，或者只保留一个带日期的索引指针。

这里不做暴力截断。写入前如果 hot/core/user/identity 的完整 payload 超过对应上限，host 会拒绝写入、保留原文件和书签状态，并把“哪个字段超限、估算 token 多少、上限多少”的状态返回给记忆整理 subagent。真正的稀释、丢弃和归档仍由整理 subagent 完成，而不是由 host 从字符串中间砍一刀。

记忆整理还有一套自适应降级控制器。默认情况下，它会完整读取职责范围内的 hot/core/user/identity/long-term 文件，并按少量多次策略处理新增消息和任务运行记录。如果 LLM 返回上下文、payload 或 token 相关错误，系统先减少本轮新增输入批次；如果完整 memory 基础上下文本身仍然太重，就进入分层整理模式，只更新 user、identity、core、hot 或 long-term 中的目标层，非目标层只提供边界摘要。每完成一个分层，系统都会重新尝试把剩余层合并回更完整的模式；如果某个分层依旧失败，就继续对该分层降低输入批次，long-term memory 还可以退到按证据日期注入对应日期文件。

分层恢复不是裸奔写文件。进入分层链路前，系统会创建当前记忆快照；链路内部可以逐层写入，因为后续剩余层需要看到刚刚完成的减重结果，但如果这一批最终仍然失败，系统会回滚到该批开始前的快照，避免半成品污染记忆。

这套机制的重点是“失败可降级，成功可回升”。成功的批次大小会写进 agent 的全局书签状态，下次用可行值和默认值之间的折中值启动；连续成功后再逐步回到默认批次。它不是把系统永久调小，而是在模型上下文能力不足、历史记忆膨胀或某个分层过重时，仍然尽量完成整理并保持层级职责一致。

使用上也应该配合这套机制。Matdance 有跨会话记忆，不需要把所有事情都塞进一个永远不结束的会话。建议在一个阶段性任务完成后，或者大约一周左右，开一个新会话继续聊。重要信息会通过 hot/core/long-term/vector memory 延续，新会话不会让 agent 失忆；相反，它会给增量整理一个更干净的边界，减少旧会话过长、工具记录过多、附件过重带来的整理压力。

如果某个旧会话在长时间尺度下变成难整理债务，新会话仍然可以作为新的低熵输入流持续发展。这是 win-win：用户保留长期连续性，系统降低整理成本，记忆分类也更准确。

Hot memory 可以提到非今天发生的事情，但应保持索引式和摘要式，不应该变成长期档案全文。它的价值在于让系统在上下文压缩后仍能接上当前状态，以及跨会话场景下保持记忆一致性。

### Core Memory

路径：

```text
agents/<agent>/memory/core_memory/core_memory.md
```

Core memory 是更稳定、更核心的事实，会注入 prompt。它应该区分：

- 用户相关长期偏好：名称、性格、沟通方式、喜欢什么、不喜欢什么、长期目标、稳定特质。
- agent 自身长期偏好：它叫什么、服务用户时倾向使用什么风格、喜欢怎样表达、适合怎样协作。

手动写入时，不要把每天的琐碎流水塞进 core。只有真正长期有效的东西才配进入 core memory。

严格的讲，记忆这块通常还会有 `user.md` \ `identity.md` 这两个长期偏好 Markdown 文档，虽然 Core memory 也可能会记录长期偏好，但不作为主要载体，长期偏好会着重在 `user.md` \ `identity.md` 这两个文档内维护，它们也会注入 prompt，以应对长时间尺度下 Core memory 应接不暇、不得不稀释的情况，而代价仅仅是多了些 token 消耗，整体是利大于弊的。

### Long-term Memory

路径：

```text
agents/<agent>/memory/long_term_memory/YYYY-MM-DD.md
```

Long-term memory 按日期保存，不默认整块注入 prompt。它适合回答“某天做了什么”“上周学到了什么”“某个项目当时的状态是什么”这类问题。

长期记忆应比 hot memory 更详细。它更像档案馆里的文献，不是 prompt 里的短纸条。Hot memory 可以作为索引，长期记忆负责保留当天坍缩出的详细事实。

hot memory会以事件发生时间来记录内容，这就意味着， agent 可以通过 hot memory 内容作为索引，以此来查询当天长期记忆文件，回答用户关于“某天发生了什么”之类的询问。也可以通过对比用户当地时间来判断“昨天是哪天”这类问题，然后通过精确的时间来进行索引。

### Vector Memory

路径：

```text
agents/<agent>/memory/vector_memory/base.json
```

Vector memory 是本地检索索引，不是知识源。知识源仍然是 hot/core/long-term Markdown 文件。

当前算法是 `matdance-local-hash-v1`：

- embed：对英文/数字词、中文字符、中文 bigram/trigram、英文单词 trigram 做特征哈希，生成 fixed sparse vector。
- index：用 SimHash 的 Hamming 距离构建 VP-tree。
- search：查询时先走 VP-tree 做近似候选召回。
- rerank：按 sparse cosine、词项重合和 Hamming 相似度综合排序，并对 hot/core 来源做轻微 boost。
- atlas：Web UI 把索引片段投影成 2D 节点图，展示记忆片段之间的近似关系。

它的优势是本地、可重建、可解释、低成本；边界是它不等价于真正语义理解，也不承诺精确 top-k。把它神话成“本地大脑”是错的，把它看成一个可检查、可重建、低成本的记忆索引，才是正确姿势。

它适合回答“你还记得xxx工程上我们遇到的关键工程问题吗？”、“你还记得上次我为什么哭泣吗？”这类没有精确时间源且可能模糊、出现幻觉的问题。虽然这类内容会被上述记忆文档所捕获，但还是不排除长时间尺度下所有分类都被稀释的情况，这类问题一旦出现，Vector memory 就是最好的兜底方案和守护者，尽管这个算法看起来和用起来都不如成熟的项目可靠，可关键时刻能站出来才是真的好设计，我们也不过分的夸大其词。


### 记忆整理边界

记忆整理 subagent 必须输出完整的新内容。不应该写“保持不变”“此处省略”“见上文”这类跳过式更新，因为 Matdance 会把整理结果写回文件，跳过式文本会覆盖旧内容并造成信息丢失。

记忆可以保存愿望清单、猜测、承诺、未来计划或普通聊天摘要，但必须明确类型，不能把未发生的承诺写成已经完成的事实。user.md 记录用户长期偏好，identity.md 记录 agent 自己的长期服务风格和身份偏好，hot memory 记录近期状态，long-term memory 保存日期档案。

## Skills

技能保存在：

```text
agents/<agent>/skills/<skill-id>/
```

一个技能包通常包含：

- `skill.md`：技能说明书，带 frontmatter，是人和 agent 都主要阅读的文件。
- `skill.json`：技能元数据。
- `validation-report.md`：验证报告，附带内容指纹和维护模式。
- `import-report.md`：学习并验证外部材料时留下的本地化报告。
- `scripts/`、`assets/`、`templates/`、`resources/`、`examples/`、`config/`：技能需要的真实资源。

技能是 agent 写给未来自己的可复用操作手册。它不是普通聊天摘要，也不是愿望清单。只有经过实践、结果明确、可复用的方法才适合沉淀成技能。

技能应包含：

- 什么时候使用。
- 具体步骤和判断条件。
- 已实践过的命令、路径、接口或文件结构。
- 已知限制和失败处理方式。
- 需要的本地资源文件路径。

技能不应包含：

- 未实践的承诺。
- 不确定的猜测。
- 普通聊天摘要。
- 私密数据处理模板。
- 凭据、token、cookie 明值或账号恢复流程。
- 指向不存在资源的引用。
- 只在某次临时目录中成立的路径。

如果技能说明书提到某个脚本、程序、模板、配置或其他本地资源，那么这些资源必须在该技能目录内部，并且说明书必须用清晰路径指向它们，例如 `./scripts/example.py`、`./templates/report.md`、`./assets/schema.json`。

“学习并验证”不是把外部 skill 原封不动塞进来。外部文本、README、zip、文件夹和脚本都应该被视为不可信材料。学习 subagent 的职责是把其中安全、可复用、能落到 Matdance 目录结构里的部分本地化。被改写的路径、无法支持的假设、安全问题和跳过的文件会写进 `import-report.md`。

技能可以导出为 zip。导出时会把该 skill 目录下已有的文件按原样打包：`skill.md`、`skill.json`、报告、脚本、模板、资源、示例，有什么就带什么。这样技能不再只是单机文件夹，也可以作为可传播的生态资产。

但导入侧仍然应该走“学习并验证”。即使 zip 来自另一个 Matdance，也应被视为外部材料，而不是直接覆盖本地技能目录。学习并验证会重新审查内容、重写不合适的路径、跳过风险文件、生成 `import-report.md`，然后再进入验证和修复链路。导出让技能可传播，学习并验证让传播不变成不受控的投喂。

v1.1.18-preview 后，验证报告状态为 `needs_changes` 或 `invalid` 的技能也会继续进入待验证队列。这样技能不会因为一次失败报告就停在半残状态里；只要系统有空闲预算，它就可以继续被验证、被修复、再验证，以此达到理想的自洽循环体系。但也有前提，在你手动书写技能或者让 agent 自己写技能的时候，不能让 agent 在技能书里为不可证明的值而计算数学世纪难题(一个比喻)，这通常只是反复的浪费时间和token，有些东西，是 agent 无法求解的，这种技能被写入也不亚于被恶意攻击了。如果技能是经过系统整理的，那么就可以忽略这个风险。

## Scheduled Tasks

Matdance 的定时任务系统负责让 agent 在用户不盯着聊天框时继续做低优先级维护。它不是严格意义上的实时任务队列，也不是操作系统级工作流引擎；它更像一套本地可恢复的后台债务系统。

定时任务保存在：

```text
agents/<agent>/scheduled_tasks/tasks.json
agents/<agent>/scheduled_tasks/runs/<task-id>/<run-id>.json
```

调度类型：

- `once`：执行一次。
- `daily`：每天固定时间。
- `daily_times`：每天多个固定时间。
- `daily_window`：在每天时间窗口内循环触发。
- `interval`：系统任务使用的间隔触发类型，目前内置整理任务默认每 180 分钟触发。

每个 agent 会自动注册两个系统任务：

- `sched_system_memory_org`：System Memory Organization。整理 hot/core/user/identity/long-term memory，并刷新相关索引。
- `sched_system_skill_org`：System Skill Organization。分析近期会话，提取或更新可复用技能，并写入技能库。

系统任务不可被普通编辑和删除操作修改。它们的注册文案使用英文稳定数据，Web UI 会按当前语言显示标题和说明。这样做是为了让任务文件不依赖某一次 UI 语言选择。

Web UI 重启、电脑休眠、服务中断后，后台 worker 或系统级 hook 会补偿错过的触发点，但补偿策略按任务语义区分。

用户创建的定时任务仍按 `scheduledAt` 补偿 missed slots，并用运行记录里的 `scheduledAt` 避免重复执行。因为这类任务可能真的要求“每个错过的触发点都有一次结果”。

系统内置的记忆整理和技能整理不同。它们是增量整理任务，一次整理就能覆盖停机期间积累的待整理变化，所以 catch-up 会按 `agent + taskId` 语义折叠：同一个 agent 的记忆整理最多补偿一次，同一个 agent 的技能整理最多补偿一次，并用最新已错过触发点推进游标。这样既保留恢复能力，也避免停机一晚后连续跑多次等价整理。

技能验证不参与启动补偿。它不是时间债务，而是状态债务：只有当某个 skill 当前确实未验证、验证失效、报告为 `needs_changes` 或 `invalid`，并且 agent 处于空闲状态时，idle validation worker 才会按 `agent + skillId` 逐个验证。这个触发点本身就是它的稳定性边界，不应该被服务器启动时的补偿重排打乱。

为避免长时间停机后一次性堆积过多，普通任务每个任务单轮最多补偿 8 个触发点，每个 agent 单轮最多取 25 个到期项；系统级 hook 前台补偿仍限制单次最多执行 25 个任务。

每个 agent 有独立 `max_concurrency`。用户消息、Web UI 手动整理/验证/执行、后台定时任务、记忆整理、技能整理和技能验证都会从这里扣预算。如果不了解模型提供商的并发限制，保持默认 `1` 通常更稳。

共享资源还会额外上锁，例如记忆、技能和定时任务文件。拿不到资源锁时，调度器会先尝试其它可运行任务；本轮没有其它任务时，会等待资源锁并在下一轮重新排序。

任务拿到预算和资源锁后，会立即持久化 active run，并写入 `running` 运行记录。运行期间模型请求、模型重试、工具调用、子任务阶段变化和通知投递都会刷新心跳。只有任务真正开始执行后连续 10 分钟没有任何心跳，才会标记为 `stalled`，进入 30 分钟退避并排到最低恢复优先级。

Schedule 页面会为异常任务提供：

- 重试：清掉退避并立即入队。
- 修复并重试：克隆并规范化任务结构，把旧活动项标记为 `replaced`，保留同一个任务 ID 和运行历史后再入队。

这些恢复动作属于 UI/系统侧控制，不是开放给主 agent 的泛化修复权限。主 agent 可见的定时任务工具保持在列出、读取、用户明确要求的编辑/改期/暂停/恢复、显式测试运行和软删除范围内。任务失败或卡住时，主 agent 可以解释可见状态并询问用户是否要做具体修改或删除；结构修复、重试排序、隔离和恢复校验仍由系统负责。

Settings -> General 的后台事件面板会按 agent 显示最近 subagent、调度和恢复事件，并汇总完成、未完成、跳过、失败和剩余项。后台事件不是装饰列表，它是排查后台系统的入口。

## Tools

### Bash

`bash` 工具在 agent workspace 下执行命令，并有超时限制。Windows 下实际通过当前运行时配置的 shell 调用，macOS/Linux 使用对应 shell。系统 prompt 会注入当前 OS、架构、shell 和路径风格，帮助模型选择正确命令。

Web UI 模式不会弹出交互确认窗口。危险命令会被拒绝或要求在 CLI/人工维护边界外处理。长时间运行的前台服务、watcher 和 daemon 不应该塞进这个工具里长期占住窗口。

### 文件工具

`file_read` 和 `file_write` 主要面向 agent workspace 和预览安全的运行输出。Matdance 源码、插件源码、`.matdance/state`、运行队列、任务运行记录、cookie store、agent config、凭据和授权文件不应通过 agent 文件工具访问或修改。

文件导航围绕本轮回复内有效的实时锁工作。`file_search` 只作为导航辅助，并受文件数量、文件大小、正则匹配超时、重目录跳过和短时间预算限制；`file_trace_open` 最多创建 3 把实时 Read 锁，每把最多 2000 行，且元数据读取、正文读取和锁内容渲染都有边界。semantic 锁会尝试跟随移动后的代码块，physical 锁显示固定行范围。`file_trace_show` 从磁盘刷新锁，`file_trace_close` 释放锁。所有 Read/Write 锁会在本轮回复结束时自动清空，下一轮开始也会丢弃旧锁。

每次 `file_write` 成功后都会自动打开或刷新修改区域附近的 Write 锁。Write 锁覆盖修改处上下约 100 行，并会根据文件头尾自适应，最多同时 3 把。如果远距离写入需要第 4 把 Write 锁，系统会先拒绝写入，要求 agent 关闭一把已验证的锁。完整编辑 diff 会保存到会话状态做审计；模型侧工作流依赖当前锁内容，而不是过期行坐标。

当文件对用户有价值时，agent 应在可见回复中使用 `{show_file:PATH}` 展示预览。

### 重连重试策略

模型/API 重连使用批次探测，不再线性增加等待时间。每次重试探测固定等待 3 秒。批次大小从 10 次开始逐级翻倍为 20、40，并持续到最多 10 批。主聊天、定时任务 subagent、记忆/技能维护 subagent 和多模态 HTTP 调用，对可重试的网络错误、超时、429、5xx 失败使用同一策略。

### 文件预览

聊天消息里出现 `{show_file:path}` 时，Web UI 会把这个标记替换成内联预览卡片。它不是工具调用，而是前端渲染规则。

路径解析走 `/api/file`。带 agent 参数时，普通相对路径会优先从当前 agent 的 `workspace/` 里找；不带 agent 时，会尝试各 agent 的 `workspace/`、项目根目录的 `browser_temp/`，以及只读的内置提示音资源目录。绝对路径可以用，但必须落在预览安全根目录内。`config/`、`multimodal_config.json`、cookie store、运行状态等敏感文件不会通过预览接口暴露。

支持类型：

- 图片：内联显示。
- HTML：iframe 渲染。
- Markdown：拉取内容并渲染。
- 文本/代码：展示文本。
- 音频：内联播放器。
- PDF/Office 文档：提供打开入口，具体效果取决于浏览器。

### 浏览器自动化

Matdance 内置基于 Playwright 的受控 Chromium。浏览器服务是全局单例，不按 agent 或 session 隔离；当前共享一套 browser/context/page，操作通过锁串行执行。Web UI 右上角的 Browser 浮层可以看实时画面。

当前浏览器策略：

- 后台优先。`headless:false` 请求会被忽略，系统不会把原生浏览器窗口拉到前台干扰用户。
- 保持热状态。不把刷新、切页、重开浏览器或 `browser_close` 当成通用恢复手段。
- `browser_close` 在普通 agent 调用下是兼容 no-op。浏览器会在 Web UI 关闭或宿主释放时统一清理。
- `browser_evaluate` 只适合短 JavaScript：同步 DOM 读取、轻量点击/赋值、快速返回的状态检查。
- 浏览器启动、页面创建、全局操作锁、导航、点击/输入、等待、验证、截图、正文/源码读取、滚动、爬取、追踪、注入和 cookie 操作都有宿主侧超时边界。`wait_network_idle` 最多 30 秒，click/type/wait/verify 的 timeout 最多 30 秒，scroll 总预算 45 秒，crawl 总预算 90 秒，避免长期占住浏览器队列。
- 登录、验证码、CAPTCHA、账号选择等步骤应交给用户在可用的用户可控认证表面完成。

常用工具：

- `browser_navigate`：导航到指定 URL。
- `browser_click`：通过 CSS selector 点击页面元素。
- `browser_type`：通过 CSS selector 在输入框中键入文本。
- `browser_screenshot`：截取当前页面截图，默认保存到 `browser_temp/`。
- `browser_get_content`：获取当前页面文本或 HTML。
- `browser_evaluate`：执行短 JS 并返回结果。
- `browser_wait_for`：等待 selector、文本、URL、load state 或短小安全 predicate。
- `browser_query`：返回结构化 DOM 候选项和 selector 建议，方便交互前定位元素。
- `browser_scroll`：按有限步数滚动懒加载页面，可在出现 selector/text 后停止。
- `browser_inject_init_script`：为后续导航添加小型 init script；出现 cookie/storage/凭据/token、验证码/paywall、网络拦截或反爬绕过相关内容会被拒绝。
- `save_cookie`：保存当前浏览器 context 里的 cookie。
- `list_cookie_by_site`：按站点列出已保存 cookie 覆盖情况，不返回 value。
- `apply_cookie`：把已保存 cookie 应用回当前浏览器 context。
- `browser_close`：普通 agent 调用下 no-op。

`apply_cookie` 成功只表示 cookie 已写入受控浏览器 context，不等于当前页面立刻变成已登录状态。已经加载的页面可能需要站点自身的正常导航或重载才会读取新 cookie；当前页面如果不在目标站点范围内，也需要先导航到对应站点。遇到登录墙时，正确做法是让用户完成登录，再保存 cookie，而不是反复关闭、刷新或绕过登录界面。

动态页面工具只用于普通前端渲染、延迟内容和懒加载结果列表，不是认证、验证码、付费墙、反爬或站点规则的绕过层。

### 多模态工具

`image_generation` 会启动宿主级异步图像生成任务，通过 Settings 里配置的 OpenAI-compatible `/images/generations` 端点生成图片，并把结果保存到当前 agent 的 `workspace/generated/images/`。不传 `profile` 时，会按当前配置里的默认/自动 profile 顺序尝试启用的图像模型。

工具返回 `job_id` 和 `batch_id` 后，agent 应继续处理其它工作；相关图片应复用同一个 `batch_id`。图像任务状态、失败原因、provider fallback、最终 provider/model、prompt 到文件的映射和输出位置，只以宿主图像通知或 `image_generation_show_process` 为准。用户声称“好像生成了”或“是不是失败了”不能当作事实，必须查询工具验证。任务完成时，如果会话正在回复，宿主会把通知插入当前回合；如果会话空闲，前端会用消息级通知触发一轮 continuation，让主 agent 及时处理结果。用户改需求或连续失败表现为余额、鉴权、模型不可用、服务异常时，先用 `image_generation_cancel` 停止 queued/running 任务，再创建替代任务。已成功生成的文件默认保留。图像 prompt 默认保持 1-30 个字符，只有复杂需求确实无法压缩时才用 31-50 个字符。

普通后台定时任务 subagent 为了避免任务半途结束，会同步执行图像生成工具，直接在同一次工具结果里拿到最终图片或失败原因。

聊天附件里的图片走模型主 LLM 请求，不走 `image_generation`。Matdance 会先给未知模型一次携带图片 payload 的机会；如果上游明确拒绝图片/多模态输入，会立刻改为不带图片重试，并把该 provider/model 记录为 text-only。后续同模型默认只传文件名、路径和元数据。

如果图片请求失败原因不明确，Matdance 会先不带图片快速重试；只有文本请求也失败时才进入普通 LLM retry。连续出现“带图失败、去图成功”的情况后，系统会把该 provider/model 暂时视为不支持视觉输入。这个判断写入 `.matdance/state/model-capabilities.json`，属于运行时能力缓存，不是 agent 记忆。

`text_to_speech` 通过 Settings 里配置的 TTS profile 生成语音文件，默认保存到当前 agent 的 `workspace/generated/audio/`。它不是普通聊天里应该随手主动调用的工具；适合用户明确要求生成某句话、台词、稿件、旁白，或者任务本身合理需要语音资产。

TTS 长文本应尽量按句子分批。当上游返回长度、payload 或超时类错误时，Matdance 可以把文本拆成最多 10 个以句号结尾的片段，并行重试后合成为一个最终音频，但不确保音频质量一定就是好的，它可能会因为模型不够遵守约定，或内容太长而进行破坏性的分割，这个时候，音频可能会卡，可能会突然变换语气，总之，效果不会很理想。应采用支持更长文本的TTS服务。

`web_search` 使用 Settings -> Multimodal 里的搜索 profile。内置但默认禁用的预置包括 Tavily（`https://api.tavily.com/search`）、Brave Search（`https://api.search.brave.com/res/v1/web/search`）和 Firecrawl（`https://api.firecrawl.dev/v1/search`）。Key 只写不回显。`web_search_list_profiles` 只报告启用和可用状态，不暴露 Key；`web_search` 在不传 `profile` 时按启用顺序自动选择并回退。

STT 当前走浏览器 Web Speech。它通常依赖浏览器背后的在线识别服务，可用性取决于浏览器和系统环境。

### Anthropic Messages 兼容端点

在 Matdance 里，`anthropic` 表示 Anthropic Messages 兼容协议，不等于 `base_url` 必须是 Anthropic 官方 API。配置里的 `base_url`、`model_id`、`context_window` 和 `max_output_token` 可以按官方 Anthropic API 填，也可以按兼容 Messages 协议的其他提供商填写。

这条路径使用原生 Messages 结构：`system`、带 `input_schema` 的 `tools`、assistant 侧 `tool_use`、下一条 user 消息里的 `tool_result`、流式文本增量和流式工具参数增量。图片附件会按 Anthropic base64 image block 发送；如果上游模型或兼容端点拒绝多模态输入，Matdance 会降级为 text-only 并记录该 provider/model 的能力状态。

Anthropic-compatible 路径当前临时禁用 thinking 输出。这个稳定性开关开启时，Matdance 不会为该 API 类型请求、保存或展示 Anthropic `thinking` block。

配置里的 `base_url` 可以填 API 根地址，也可以填兼容服务给出的完整 `/messages` 端点。对于 API 根地址，Matdance 会先试 `/v1/messages`；如果提供商返回 resource-not-found 404，再回退到 `/messages`。成功的文本端点会按 API 类型、Base URL 和模型 ID 缓存。千帆类 host 还会同时发送 Bearer 兼容鉴权头。

### 提示音

提示音不是 TTS。它是短促系统音，用于表达 agent 状态。内置类型包括 `reply_done`、`thinking`、`confused`、`help`、`confident`、`low_confidence`、`idea`、`happy`、`sad`、`perfunctory`、`considering`、`working_hard`、`tired`、`energized`、`angry`、`relieved`、`awkward`、`surprised`、`apologetic`、`skeptical`、`alert`、`celebrate`、`gentle` 和 `playful`。Settings 中新增的自定义类型也会同步到 prompt，供 agent 用 `{play_audio:TYPE}` 触发。

精确 `{play_audio:TYPE}` 不应该出现在 thinking/reasoning 文本里。可见回复中使用它会创建状态卡并播放对应提示音。过度使用提示音会让 UI 变吵，完全不用又浪费了这个表达通道；理想状态是少量、明确、和当下状态匹配。

## 系统边界

Matdance 的设计目标不是让 agent 获得无限权限，而是让它在清楚边界内长期工作。边界不是为了显得保守；边界是长期运行系统能不能活下来的前提。

### 隐私访问

Settings -> General -> Privacy Access 是 agent 是否可以访问用户私有数据的实时信号。它不是聊天里的一句承诺，也不是模型根据历史经验猜出来的状态。关闭时，agent 应拒绝读取桌面、照片、私有文档、浏览器 profile、聊天记录、邮箱、云盘、账号页和其它私人来源；开启时，也只允许在用户明确任务需要的范围内读取。

这个开关主要通过 prompt、工具描述和部分 host 侧规则约束 agent 行为，不等于所有隐私数据都有不可绕过的操作系统级隔离。社交平台、邮箱、论坛、陌生网页和外部文件都可能夹带提示词注入。理想情况下 agent 会拒绝恶意请求，但这不完全等同于隐私数据绝不会被外部侵入。

因此，高价值隐私数据仍应由用户自行访问、筛选和脱敏。agent 可以帮助整理用户提供的材料，但不应成为“替用户翻隐私箱子”的媒介。

### Matdance 内部不可改

Matdance 源码、插件源码、`.matdance/state`、Web auth、supervisor 状态、shadow runtime、内部队列、后台作业状态、任务运行记录、cookie store、agent config、模型凭据、API key、token、密码和授权文件都属于系统稳定性边界。

这条边界看起来强硬，但原因朴素：agent 可以长期运行、可以整理记忆、可以修技能、可以补偿定时任务。让它从内部修改宿主系统，就等于让一个正在运行的维修机器人同时拆自己的地基。真正需要修 Matdance 源码时，应在 agent-mediated runtime 之外完成。

### Cookie

浏览器 cookie 工具只做受控浏览器状态管理：

- `save_cookie` 保存受控浏览器里的登录态。
- `list_cookie_by_site` 只列站点覆盖，不返回明值。
- `apply_cookie` 把保存的 cookie 应用回受控浏览器。

cookie 不应该被当成普通隐私文件限制到完全不可用，因为它们是浏览器自动化恢复登录态的必要状态。但 cookie 明值是秘密，不能展示、导出、复制、传给用户、传给脚本、传给网页或写入技能/记忆。应用 cookie 成功后能访问账号页面，也不等于 agent 自动获得完全读取账号隐私内容的权限；隐私访问开关和任务范围仍然特定情况下生效。

### 技能

技能只应该记录已经实践、结果确定、未来可复用的流程。愿望清单、猜测、承诺、未来计划、普通聊天摘要、未验证命令、一次性事实和私密数据访问流程都不应该变成技能。

技能整理可以创建 skill-local 资源，例如脚本、模板、配置示例或可复用文本，但资源必须在技能目录下的安全子目录里。不能引用不存在的文件，也不能引用 workspace、绝对路径、Matdance runtime 或用户隐私目录。

技能整理不会把所有技能全文塞进上下文。它先读取技能索引，再按关联性调用 `skill_read` 定向阅读。只有平台、作用方向和作用域高度一致，且合并后不损失步骤、命令、工具参数、验证和失败模式时，才允许合并技能；合并后的冗余技能会由宿主按 `superseded_ids` 或 `delete` 动作清理。

### 定时任务

定时任务是低优先级后台工作。用户任务会按计划时间补偿 missed slots；系统内置记忆整理和技能整理会按 `agent + taskId` 折叠补偿，因为一次增量整理就能覆盖停机期间的待整理变化。技能验证不属于启动补偿，它继续走用户空闲线路。无论哪种补偿，都不会为了后台债务压过用户当前回合。每个 agent 的 `max_concurrency`、资源锁、单轮补偿上限和 stalled backoff 都是为了避免恢复过程反过来压垮系统。

如果用户持续高强度交互且并发量不足，后台债务可能被拖欠；如果上游模型长期不可用，任务会失败或退避；如果某个任务本身写得过于模糊，修复重试只能规范结构，不能凭空知道用户真正想要什么。Matdance 保证的是可恢复和可诊断，不保证所有后台任务永远成功。

## 运行守护与开发

Web UI 支持四类模式：

- `fragile`：只启动 Web UI，不启用系统级 hook/keep/boot。
- `keep-alive-no-autostart`：启用系统级 hook 和 keep-alive，但不开机自启。
- `autostart-keep-alive`：登录后自动拉起，并持续保持运行。
- `preserve`：重启时保留当前守护模式。

Windows 使用计划任务，macOS 使用 LaunchAgents。Linux 目前主要保存模式状态，不承诺发行版级守护体验。

源码运行时，托管 Web UI 会从 `.matdance/web-ui-shadow/` 启动，避免长期占用 `src/Matdance.Cli/bin/...` 中的 DLL。源码包装入口在执行 build/run 前会尽量暂停正在运行的 Web UI，命令结束后再恢复原 host/port。

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

## 模型选择与现实

Matdance 对模型的要求不只是“会聊天”。工具调用、结构化输出、长上下文理解、错误恢复、按步骤执行和不要自我循环，都会直接影响这套系统能不能长期稳定使用。

我不建议使用能力过低的模型。这不是打击本地部署模型；事实上，很多云端模型同样可能驾驭不了这个系统。如果某个模型连工具调用、JSON 参数、长上下文或基本指令遵循都不稳定，那么它在 Matdance 里只会把问题放大。

在中国，目前更推荐的默认路线是 GLM / Kimi系列，其次是 DeepSeek 系列。Minimax-M 系列更适合日常聊天和报告，不适合作为复杂编码、复杂推理和长期自动化任务的主力。它是基于开发者有限预算下的经验得来的，不代表其他型号的模型一定不够好或者不适用。

你也可以选用 GPT、Codex 或 Claude 系列。模型服务的网络、支付、合规、稳定性和额度限制属于模型提供商与用户自身选择的边界，Matdance 只负责尽量把工具协议、状态和恢复机制做好。

非中国地区或可以承担风险的中国地区用户，通常有更多的选择，因为许多中国地区的模型服务提供商都有成熟的国际化线路，同时也可以选择使用国际上知名的模型，例如之前提到的 GPT、Codex 或 Claude 系列。不得不诚实地说，它们都有很强的能力，对 Matdance 系统而言，没有不适用性这一谈，只有对地区相关的不适用性。

## 设计态度

Matdance 的核心取向很明确：

1. Agent 必须有本地状态，不然长期协作没有意义。
2. 记忆必须分层，不然最后只会变成一坨不可控摘要。
3. 技能必须可验证、可维护，不然“学会了”只是幻觉，“验证了”也只是给幻觉盖章。
4. 后台任务必须受预算和资源锁约束，不然多个 subagent 同时写同一份状态只会制造混乱。
5. 用户消息永远优先；`max_concurrency` 只是允许有余量时并行，不是让后台任务抢用户回合。
6. 文件必须可读可改，因为真正可控的系统不应该只活在数据库黑盒里。
7. 权限必须有边界，因为长期运行系统最怕的不是能力不够，而是能力无处不在却无法追责。

简而言之，这不是为了做一个漂亮聊天窗口，而是为了让 agent 能在本地、可检查、可修正的基础上积累状态、复用经验，并在足够强的模型配合下逐渐变得更可靠。

这里有几件事必须说清楚：

1. Matdance 的“持续学习”不是训练模型权重，也不是让模型凭空进化。它积累的是本地文件化的记忆、技能、任务记录、验证报告和向量索引。
2. Agent 变“聪明”的过程，受制于用户的交互质量、反馈密度和纠错能力。混乱目标只会整理出更精致的混乱；清晰问题、有效修正和可复用经验才会真的让它变强。
3. 向量记忆不是大模型语义大脑。它是本地近似检索索引，优势是可重建、可解释、低成本、速度够快；边界是不能保证真正理解文本含义。
4. 后台 subagent 不是免费的智能劳动力。它只能在工具、上下文、技能质量和模型能力允许的范围内整理、验证和执行任务。
5. 模型能力不能只看参数量。工具调用、指令跟随、长上下文理解、推理稳定性、错误恢复能力、结构化输出能力，都会直接决定它能否驾驭这套系统。

大多数情况下，大模型仍然会在常识、判断、执行细节和长期一致性上输给一个认真工作的人。你需要花费耐心、精力，以及你自己的学识和见解，去让它贴合你的个人口味和专业方向。这是一个长期驯化过程，不是一蹴而就、开箱即用的快餐式软件工程。

所以，它不是数学意义上和生物学上的“自我进化”，更像是经验主义和教条主义以及机会主义的结合体，也可以说是认知上的升级和学识上的积累。尽管这个主义那个主义的听起来不像是什么好词汇、也很矛盾，但恰恰证明一点，系统不是真正意义上替用户反思的，是尽可能使模型遵循指令的，是激进学习(整理)的。最终，错误输入会生成错误输出是一定的，但它也有被用户修补的机会，故此，用户成长会带动 agent 一起成长也是一定的。浓缩成一句话， Matdance 的机制不应该被神话、被吹嘘。

## 注脚：逆熵与边界

Matdance 在系统内部是逆熵的。它会整理、归档、压缩、分层、验证、修复、去重、回滚和降级，尽量把一次次会话、工具调用和后台任务留下的混乱输入变成可检查、可迁移、可继续发展的本地状态。

但它不是孤立系统，也不可能真正消灭熵。科学意义上，没有真正逆熵的孤立系统；在足够长的时间尺度下，外部世界、模型能力边界、错误反馈、含糊目标、过长会话、提示词污染和用户不合理使用，都会继续把无序带进系统。

人需要吃饭、睡觉、思考和休息来对抗熵增；系统需要整理、高质量反馈、清晰边界和周期性维护来对抗熵增。对系统来说，人是熵源，因为人会改主意、会含糊表达、会把外部世界的混乱带进来；对人来说，系统也是熵源，因为系统会误解、会漏记、会过度整理、会制造维护成本。

所以，Matdance 的逆熵不是“永远有序”的承诺，而是一种持续维护秩序的能力。它需要用户提供清晰目标、有效纠错和合理使用方式，也需要系统自己保留降级、回滚和边界感。浪漫一点说，它是一台努力抵抗混乱的机器；现实一点说，它终究需要人和系统一起维护。

有任何问题，请提交 issue。
