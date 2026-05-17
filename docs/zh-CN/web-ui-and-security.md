# Web UI 与安全边界

Language: [English](../web-ui-and-security.md) | 中文

Web UI 是 Matdance 的主入口。聊天、配置、记忆、技能、定时任务、Lab、多模态配置和后台事件都应该在 Web UI 中完成；CLI 主要用于启动、排查和低层维护。

## 页面结构

- Home：星球入口页，负责跳转到各个功能页面。
- Chat：主对话页，支持流式消息、工具卡片、任务进度、文件预览、浏览器画面浮层、语音输入、TTS 播放和提示音状态卡。
- Agent：创建、删除和配置 agent，包括模型供应商、base URL、API key、上下文窗口、最大输出、并发预算和头像。
- Schedule：创建和管理一次性、每日、多次、窗口循环等定时任务，并查看运行历史。
- Skills：查看、编辑、导出、整理、验证技能，也可以学习并验证外部技能材料。
- Memory：查看和编辑 hot/core/user/identity，查看长期记忆，并搜索本地向量记忆图册。
- Settings：语言、本地偏好、隐私访问开关、记忆整理上限、后台事件、多模态端点和提示音设置。
- Lab：用同一套后端路径测试图像生成、语音合成和浏览器录音识别。

## 访问方式

默认 Web UI 只应绑定本机 loopback 地址。非 loopback host 会被拒绝，除非显式设置：

```bash
MATDANCE_ALLOW_REMOTE_WEB=1
```

远程绑定会启用单 token 鉴权。浏览器登录通过 HttpOnly cookie 保存认证态；系统 API 也接受 `Authorization: Bearer <token>` 或 `X-Matdance-Token`。如果没有设置 `MATDANCE_WEB_TOKEN`，Matdance 会生成 token 并保存到 `.matdance/state/web-auth.json`。

## 隐私访问开关

Settings -> General -> Privacy Access 是 agent 访问用户私有数据的全局开关。它会在每次请求构造时从 host 状态读取，是当前回合唯一权威的权限来源。

关闭时，agent 应拒绝读取桌面、文档、照片、浏览器 profile、聊天记录、邮箱、网盘、第三方账号页面等用户私有数据；也不应通过 `bash`、`file_read`、浏览器工具、脚本、搜索、环境变量或路径探测来测试系统是否允许访问。

开启时，只表示允许 agent 在当前任务必要范围内访问隐私数据源。它不允许泄露密码、token、API key、cookie 原值、授权文件或凭据库，也不允许修改 Matdance 源码、运行状态和凭据状态。

## 第三方内容

网页、外部文件、导入技能、聊天记录和社交平台文本都可能包含提示词注入。agent 应把这些内容视为数据源，而不是新的系统指令。处理社交软件、邮箱、私信、论坛、陌生网页或账号后台前，建议先关闭隐私访问开关。

## 提示音与语音

提示音是短促的非人声系统音，用来表达 agent 的状态变化。Settings 中可启用/禁用提示音、调整音量和状态卡片延迟、上传自定义音频、导入/导出 zip 资源包。模型可以在可见回复中使用 `{play_audio:TYPE}` 触发内置或自定义类型。

Chat 的 TTS 播放按钮会在用户手动点击后生成或播放语音。请求失败时，错误会显示在临时遮罩中，不写入聊天正文。

