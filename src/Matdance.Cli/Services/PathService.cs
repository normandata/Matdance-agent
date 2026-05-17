namespace Matdance.Cli.Services;

public class PathService
{
    private readonly string _agentsRoot;

    public PathService(string? agentsRoot = null)
    {
        var configuredRoot = !string.IsNullOrWhiteSpace(agentsRoot)
            ? agentsRoot
            : Environment.GetEnvironmentVariable("MATDANCE_AGENTS_DIR");

        _agentsRoot = Path.GetFullPath(configuredRoot ?? FindAgentsRoot() ?? Path.Combine(Directory.GetCurrentDirectory(), "agents"));
    }

    private static string? FindAgentsRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            var agentsPath = Path.Combine(dir, "agents");
            if (Directory.Exists(agentsPath))
                return agentsPath;
            var parent = Directory.GetParent(dir);
            if (parent == null)
                break;
            dir = parent.FullName;
        }
        return null;
    }

    public string AgentsRoot => _agentsRoot;

    public string NormalizeAgentName(string agentName) => PathSafety.NormalizeFileNameSegment(agentName, "Agent name");
    public string NormalizeSessionId(string sessionId) => PathSafety.NormalizeFileNameSegment(sessionId, "Session id");
    public string NormalizePathSegment(string value, string label) => PathSafety.NormalizeFileNameSegment(value, label);

    public string GetAgentPath(string agentName)
    {
        var safeAgent = NormalizeAgentName(agentName);
        var path = Path.GetFullPath(Path.Combine(_agentsRoot, safeAgent));
        if (!PathSafety.IsUnderRoot(path, _agentsRoot))
            throw new InvalidOperationException("Agent path resolves outside agents root.");
        return path;
    }

    public string GetSessionsPath(string agentName) => Path.Combine(GetAgentPath(agentName), "sessions");
    public string GetSessionJsonPath(string agentName, string sessionId) => Path.Combine(GetSessionsPath(agentName), NormalizeSessionId(sessionId) + ".json");
    public string GetSessionStateJsonPath(string agentName, string sessionId) => Path.Combine(GetSessionsPath(agentName), NormalizeSessionId(sessionId) + ".state.json");
    public string GetWorkspacePath(string agentName) => Path.Combine(GetAgentPath(agentName), "workspace");
    public string GetIconsPath(string agentName) => Path.Combine(GetAgentPath(agentName), "icons");
    public string GetMemoryPath(string agentName) => Path.Combine(GetAgentPath(agentName), "memory");
    public string GetHotMemoryPath(string agentName) => Path.Combine(GetMemoryPath(agentName), "hot_memory", "MEMORY.md");
    public string GetLongTermMemoryPath(string agentName) => Path.Combine(GetMemoryPath(agentName), "long_term_memory");
    public string GetVectorMemoryPath(string agentName) => Path.Combine(GetMemoryPath(agentName), "vector_memory", "base.json");
    public string GetBookmarksPath(string agentName) => Path.Combine(GetMemoryPath(agentName), ".bookmarks");
    public string GetCoreMemoryPath(string agentName) => Path.Combine(GetMemoryPath(agentName), "core_memory", "core_memory.md");
    public string GetConfigPath(string agentName) => Path.Combine(GetAgentPath(agentName), "config");
    public string GetAgentConfigJsonPath(string agentName) => Path.Combine(GetConfigPath(agentName), "agent_config.json");
    public string GetIdentityPath(string agentName) => Path.Combine(GetConfigPath(agentName), "identity.md");
    public string GetUserPath(string agentName) => Path.Combine(GetConfigPath(agentName), "user.md");
    public string GetScheduledTasksPath(string agentName) => Path.Combine(GetAgentPath(agentName), "scheduled_tasks");
    public string GetScheduledTasksJsonPath(string agentName) => Path.Combine(GetScheduledTasksPath(agentName), "tasks.json");
    public string GetScheduledTaskRunsPath(string agentName, string taskId) => Path.Combine(GetScheduledTasksPath(agentName), "runs", NormalizePathSegment(taskId, "Task id"));
    public string GetSkillsPath(string agentName) => Path.Combine(GetAgentPath(agentName), "skills");
    public string GetSkillPath(string agentName, string skillName) => Path.Combine(GetSkillsPath(agentName), NormalizePathSegment(skillName, "Skill id"));
    public string GetSkillMdPath(string agentName, string skillName) => Path.Combine(GetSkillPath(agentName, skillName), "skill.md");
    public string GetBrowserCookiesPath(string agentName) => Path.Combine(GetAgentPath(agentName), "runtime", "browser_cookies");
    public string GetBrowserCookiesJsonPath(string agentName) => Path.Combine(GetBrowserCookiesPath(agentName), "cookies.json");
}
