# 记忆系统

Language: [English](../memory.md) | 中文

Matdance 的记忆不是完整聊天记录堆叠，而是按稳定程度拆成不同层级。这样可以让会话上下文压缩和长期记忆互补：当前状态进 prompt，历史细节进入档案或索引，需要时再检索。

## Hot Memory

路径：

```text
agents/<agent>/memory/hot_memory/MEMORY.md
```

Hot memory 是近期上下文和当前协作状态，会注入 prompt。它适合记录最近任务进展、刚形成的约定、当前问题状态、最近几天需要继续追踪的事实。

当前策略是：hot memory 是近期工作集，不是长期档案。普通对话里的 `memory_store` 仍然只追加，避免主 agent 在单轮对话中误删旧内容；后台记忆整理任务会完整读取职责范围内的 hot/core/user/identity/long-term 文件，并返回完整覆盖 payload。

当时间跨度越来越长且内容接近上限时，整理任务会优先保留近期且仍有操作价值的内容：当前任务、未解决问题、刚形成的约定、短期承诺和接下来还要用到的线索。已经归档到 long-term memory、可检索、低时效的旧 hot memory 可以被移出工作集，或者只保留一个带日期的索引指针。这样可以减少长时间尺度下的记忆失真，避免近期关键上下文被压成没有价值的一句话。

Matdance 不对整理结果做暴力截断。写入前如果 hot/core/user/identity 的完整 payload 超过对应上限，host 会拒绝写入、保留原文件和书签状态，并把超限状态返回给记忆整理 subagent，让它自己稀释、丢弃或归档后重新输出。

Hot memory 可以提到非今天发生的事情，但会保持索引式和摘要式，不应变成长期档案全文。

## 自适应整理降级

记忆整理优先走完整模式：完整读取职责范围内的 hot/core/user/identity/long-term 文件，再处理本轮新增消息和任务运行记录。如果上游 LLM 返回上下文、payload 或 token 相关错误，Matdance 不会把同一个超大请求原样重试到底，而会逐级缩小问题空间：

1. 完整模式下减少本轮新增输入批次，例如减少 session messages 或 task runs。
2. 如果完整 memory 基础上下文本身仍然太重，进入分层整理：只更新 `user_md`、`identity_md`、`core_memory`、`hot_memory` 或 `daily_memories` 中的一个或一组目标层，非目标层只作为边界摘要。
3. 每完成一个分层后，系统会重新尝试把剩余层合并回更完整的模式，避免永久停留在碎片化整理。
4. 如果某个分层仍然失败，再对该分层单独降低输入批次；long-term memory 还可以退到按证据日期注入对应日期文件。

分层恢复开始前会创建临时记忆快照。分层链路内部可以逐层写入，以便后续剩余层看到刚完成的减重结果；但如果这一批最终仍然失败，系统会回滚该批次的部分写入，避免留下半成品。

成功的批次大小会写入 agent 的全局书签状态。下次整理会用可行值和默认值之间的折中值启动；连续成功后再逐步回升到默认批次。这样做的目标不是永远保守，而是在失败时可完成，在稳定后可回升。

## 会话使用建议

Matdance 支持跨会话记忆，所以不需要把所有工作都堆在一个无限延长的会话里。比较稳的使用方式是：一个阶段性任务完成后，或者大约一周左右，开一个新会话继续协作。重要信息会通过 hot/core/long-term/vector memory 继续存在，新会话不会让系统“失忆”。

这样做对用户和系统都有好处。旧会话如果因为时间线太长、工具记录太多或历史附件太重而变得难以整理，不会拖住新的低熵输入流；新会话更干净，增量整理更轻，模型更容易判断哪些事实应该进入 hot memory、core memory 或长期档案。换会话不是逃避上下文，而是把上下文边界交给记忆系统维护。

## Core Memory

路径：

```text
agents/<agent>/memory/core_memory/core_memory.md
```

Core memory 是更稳定的事实和偏好，会注入 prompt。它应区分：

- 用户相关长期偏好：名称、性格、沟通方式、喜欢什么、不喜欢什么、长期目标和稳定特质。
- agent 自身长期偏好：它叫什么、服务用户时倾向使用什么风格、喜欢怎样表达、适合怎样协作。

不要把 hot memory 写进 identity，也不要把 agent identity 写进 user profile。类型边界比内容数量更重要。

## Long-term Memory

路径：

```text
agents/<agent>/memory/long_term_memory/YYYY-MM-DD.md
```

Long-term memory 按日期保存，不默认整块注入 prompt。它适合回答“某天做了什么”“上周学到了什么”“某个项目当时的状态是什么”这类问题。

长期记忆应保存足够细节，因为它更像档案馆里的文献，而不是 prompt 里的短纸条。Hot memory 可以作为索引，长期记忆负责保留当天坍缩出的详细事实。

## Vector Memory

路径：

```text
agents/<agent>/memory/vector_memory/base.json
```

Vector memory 是本地检索索引，不是知识源。知识源仍然是 hot/core/long-term Markdown 文件。

当前索引使用本地特征哈希、SimHash、VP-tree 和 rerank，不调用云端 embedding。它适合低成本模糊检索和可解释重建，但不等价于真正的语义理解。

## 整理边界

记忆整理 subagent 必须输出完整的新内容。不能写“保持不变”“此处省略”“见上文”这类跳过式更新，因为 Matdance 会把整理结果写回文件，跳过式文本会覆盖旧内容并造成信息丢失。

记忆可以保存愿望清单、猜测、承诺、未来计划或普通聊天摘要，但必须明确类型，不能把未发生的承诺写成已经完成的事实。

