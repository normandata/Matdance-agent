# Security Policy

Language: [English](SECURITY.md) | 中文

Matdance 的安全边界由本地文件布局、Web UI 绑定策略、单 token 鉴权、Settings 权限、工具描述、prompt 约束和 host 侧限制共同构成。它们能降低风险，但不能把任意上游模型变成绝对可靠的安全执行环境。

## Supported Versions

当前只维护主分支和最新 preview 版本。旧版本可能缺少最新的权限提示、浏览器自动化限制、cookie 诊断和后台任务修复逻辑。

## Web UI

默认情况下，Web UI 应只绑定 `localhost`、`127.0.0.1` 或 `::1`。如果你显式开启远程绑定，Matdance 会启用单 token 鉴权，但这不是多用户权限系统。不要把远程 Web UI 暴露给不可信网络。

## Secrets

不要在 issue、日志、截图或公开文档中粘贴 API key、token、cookie、密码、私信、邮箱内容或其它敏感原文。排查问题时应先脱敏。

## Reporting

如果发现安全问题，请优先以最小复现说明问题边界，不要附带真实密钥或隐私数据。如果没有私有报告渠道，请在公开 issue 中只描述影响、版本、复现步骤和已脱敏日志。

## Practical Guidance

- 处理社交平台、邮箱、私信、论坛、陌生网页或其它第三方文本密集环境前，建议关闭隐私访问开关。
- 远程访问 Web UI 时使用长随机 token，并避免复用常用密码。
- 不要让 agent 修改 Matdance 源码、运行状态、鉴权状态、cookie store、模型凭据或 supervisor 状态。
- 浏览器 cookie 工具只应服务于合理登录态复用，不应作为导出隐私数据的方式。

