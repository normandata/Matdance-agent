# 本地数据与目录结构

Language: [English](../data-layout.md) | 中文

Matdance 的核心数据默认在 `agents/` 下。`.matdance/` 保存本机运行时状态和依赖，不属于知识资产。

## agents

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
      hot_memory/MEMORY.md
      core_memory/core_memory.md
      long_term_memory/YYYY-MM-DD.md
      vector_memory/base.json
    skills/
      <skill-id>/
        skill.md
        skill.json
        validation-report.md
        import-report.md
        scripts/
        assets/
        templates/
    scheduled_tasks/
      tasks.json
      runs/<task-id>/<run-id>.json
    runtime/
      browser_cookies/cookies.json
      events/
      jobs/
    workspace/
      generated/
        images/
        audio/
```

`agent_config.json` 可能包含 API key，`runtime/browser_cookies/cookies.json` 可能包含可复用登录态，`sessions/`、`workspace/`、`scheduled_tasks/runs/`、`runtime/events/` 和 `runtime/jobs/` 都属于本地运行痕迹。默认不应提交这些数据。

## .matdance

典型结构：

```text
.matdance/
  deps/
    playwright-browsers/
    playwright-driver/
  state/
    supervisor/
    web-auth.json
    user-time-zone.json
    sound-cue-settings.json
  web-ui-shadow/
  bin/
```

- `deps/`：Playwright 浏览器和驱动缓存。
- `state/`：Web UI 进程状态、鉴权状态、守护状态、用户时区和提示音设置。
- `web-ui-shadow/`：托管 Web UI 的影子运行目录，避免长期占用源码构建输出。
- `bin/`：`install-entry` 生成的本地入口脚本。

## browser_temp

项目根目录下的 `browser_temp/` 是浏览器运行时缓存目录。`browser_screenshot` 和部分浏览器工具产物会保存在这里。它可能包含隐私数据，默认不应提交。

## 原子写入

会话、配置、任务、运行记录、向量索引和后台作业状态等关键 JSON 写入采用“同目录临时文件 -> 替换目标文件”的方式。它不是数据库事务，但能降低崩溃、休眠或并发写入后留下半截 JSON、幽灵运行锁和损坏状态的概率。

