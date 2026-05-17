# 定时任务与后台可靠性

Language: [English](../scheduled-tasks.md) | 中文

Matdance 的定时任务系统负责让 agent 在用户不盯着聊天框时继续做低优先级维护。它不是实时任务队列，也不是操作系统级工作流引擎；它更像一套本地可恢复的“后台债务系统”：到期、记录、执行、心跳、补偿、失败诊断、手动重试，都必须留下文件证据。

定时任务保存在：

```text
agents/<agent>/scheduled_tasks/tasks.json
agents/<agent>/scheduled_tasks/runs/<task-id>/<run-id>.json
```

`tasks.json` 是任务定义，`runs/` 是执行记录。执行记录属于运行时数据，默认不应提交。

## 调度类型

- `once`：执行一次。
- `daily`：每天固定时间。
- `daily_times`：每天多个固定时间。
- `daily_window`：在每天时间窗口内循环触发。
- `interval`：系统任务使用的间隔触发类型，目前内置整理任务默认每 180 分钟触发。

一次性任务必须选择未来时间。已经到期但还没执行的 `nextRunAt` 会保留为 due 游标，只有 worker 真正执行并写入运行记录后才推进。

## 系统内置任务

每个 agent 会自动注册两个系统任务：

- `sched_system_memory_org`：System Memory Organization。整理 hot/core/user/identity/long-term memory，并刷新相关索引。
- `sched_system_skill_org`：System Skill Organization。分析近期会话，提取或更新可复用技能，并写入技能库。

系统任务不可被普通编辑和删除操作修改。它们的注册文案使用英文稳定数据，Web UI 会按当前语言显示标题和说明。这样做的原因是任务文件属于运行时数据，不应该依赖某一次 UI 语言选择；展示语言由界面层负责。

## 补偿机制

Web UI 重启、电脑休眠、服务中断后，后台 worker 或系统级 hook 会补偿错过的触发点，但补偿策略按任务语义区分。

用户创建的定时任务按 `scheduledAt` 补偿 missed slots，并用运行记录里的 `scheduledAt` 避免重复执行。系统内置的记忆整理和技能整理是增量整理任务，catch-up 会按 `agent + taskId` 折叠：同一个 agent 的记忆整理最多补偿一次，同一个 agent 的技能整理最多补偿一次，并用最新已错过触发点推进游标。

技能验证不参与启动补偿。它是状态驱动任务：只有某个 skill 当前需要验证且 agent 处于空闲状态时，idle validation worker 才会按 `agent + skillId` 逐个验证。

为避免长时间停机后一次性堆积过多，普通任务每个任务单轮最多补偿 8 个触发点，每个 agent 单轮最多取 25 个到期项；系统级 hook 前台补偿仍限制单次最多执行 25 个任务。

## 并发预算

每个 agent 有独立 `max_concurrency`。用户消息、Web UI 手动整理/验证/执行、后台定时任务、记忆整理、技能整理和技能验证都会从这里扣预算。

这里的`max_concurrency`可以在 agent 配置页配置，表示该模型提供商给予的最大并发数，假设是5，那就意味着用户可以 边与 agent 聊天(占用 `1 x 使用同一个api key和提供商模型数量`的预算)，后台还可以跑(以 `总预算 - agent 聊天预算`)个任务。如果不了解模型提供商给予了多少并发数限制，那最好填写默认的 `1`。

共享资源还会额外上锁，例如记忆、技能和定时任务文件。拿不到资源锁时，调度器会先尝试其它可运行任务；本轮没有其它任务时，会等待资源锁并在下一轮重新排序。

## Active Run 与 Heartbeat

任务拿到预算和资源锁后，会立即持久化 active run，并写入 `running` 运行记录。运行期间模型请求、模型重试、工具调用、子任务阶段变化和通知投递都会刷新心跳。

网络或模型访问问题先按主 agent 的 LLM retry 规则自动重试，并把等待、错误类型和心跳写进诊断。只有任务真正开始执行后连续 10 分钟没有任何心跳，才会标记为 `stalled`，进入 30 分钟退避并排到最低恢复优先级。

## 手动补救

Schedule 页面会为异常任务提供：

- 重试：清掉退避并立即入队。
- 修复并重试：克隆并规范化任务结构，把旧活动项标记为 `replaced`，保留同一个任务 ID 和运行历史后再入队。

主 agent 侧暴露的定时任务工具保持更窄边界：

- `scheduled_task_list` / `scheduled_task_read`：查看任务定义和最近运行历史。
- `scheduled_task_edit`：只用于用户明确要求的改期、暂停、恢复、修改内容或调整投递目标。
- `scheduled_task_delete`：软删除任务并保留历史。
- `scheduled_task_do`：仅在用户要求测试时手动运行一次。

主 agent 不参与自动修复失败任务。遇到失败或卡住时，它可以解释可见状态、询问用户是否要改期/暂停/删除，或在用户明确要求时调用 edit/delete。结构修复、隔离、重试排序和恢复校验仍由系统侧负责。

Settings -> General 的后台事件面板会按 agent 显示最近 subagent、调度和恢复事件，并汇总完成、未完成、跳过、失败和剩余项。

## 后台事件

后台事件不是装饰列表。它把 subagent、调度器、恢复逻辑和手动补救动作写成最近事件流。每条事件通常包括 category、kind、status、message、adviceKey 和 timestamp。UI 会把 status 和 adviceKey 翻译成当前语言；运行时 message 保持稳定英文，方便跨语言排查。

常见 advice：

- `wait_for_completion`：任务仍在跑，等待完成。
- `retry_manual`：可以手动重试；连续失败时检查 API key、网络和输入。
- `review_memory`：检查记忆文件，必要时从快照回滚。
- `review_skills`：检查技能内容和资源文件，必要时重新验证。
- `review_validation_report`：打开验证报告并按建议修复。
- `review_import_source`：检查外部导入材料是否具体、可复用、安全。

这套事件流的边界也很明确：它帮助人定位后台系统发生了什么，但它不是数据库事务日志。关键 JSON 写入已经尽量采用同目录临时文件替换，仍不等于完整事务系统。需要长期保存历史时，仍然应该做外部备份。

