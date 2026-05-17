using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class AgentService
{
    private readonly PathService _path;

    public AgentService(PathService path)
    {
        _path = path;
    }

    public void Create(string name, string? model = null, string? baseUrl = null, string? apiKey = null, string? apiType = null)
    {
        var agentDir = _path.GetAgentPath(name);
        if (Directory.Exists(agentDir))
        {
            Console.WriteLine($"[agent] Agent '{name}' already exists.");
            return;
        }

        Directory.CreateDirectory(agentDir);
        Directory.CreateDirectory(_path.GetSessionsPath(name));
        Directory.CreateDirectory(_path.GetWorkspacePath(name));
        Directory.CreateDirectory(_path.GetMemoryPath(name));
        Directory.CreateDirectory(Path.Combine(_path.GetMemoryPath(name), "hot_memory"));
        Directory.CreateDirectory(_path.GetLongTermMemoryPath(name));
        Directory.CreateDirectory(Path.Combine(_path.GetMemoryPath(name), "vector_memory"));
        Directory.CreateDirectory(Path.Combine(_path.GetMemoryPath(name), "core_memory"));
        Directory.CreateDirectory(_path.GetConfigPath(name));
        Directory.CreateDirectory(_path.GetIconsPath(name));

        var config = new AgentConfig { Name = name };
        if (model != null) config.ModelId = model;
        if (baseUrl != null) config.BaseUrl = baseUrl;
        if (apiKey != null) config.ApiKey = apiKey;
        if (apiType != null) config.ApiType = apiType;
        ModelProviderCatalog.ApplyDefaults(config, preserveCustomModelId: model != null);
        config.Save(_path.GetAgentConfigJsonPath(name));

        AtomicFile.WriteAllText(_path.GetIdentityPath(name), $"# {name}\n\nIdentity configuration for {name}.\n");
        AtomicFile.WriteAllText(_path.GetUserPath(name), $"# User Profile\n\nUser profile for agent {name}.\n");
        AtomicFile.WriteAllText(_path.GetHotMemoryPath(name), "# Hot Memory\n\nRecent important context.\n");
        AtomicFile.WriteAllText(_path.GetCoreMemoryPath(name), "# Core Memory\n\nKey facts about the user and agent preferences.\n");
        new VectorMemoryService(_path).Refresh(name);
        new ScheduledTaskService(_path).EnsureSystemTasks(name);

        Console.WriteLine($"[agent] Created agent '{name}' at {agentDir}");
    }

    public void Delete(string name)
    {
        var agentDir = _path.GetAgentPath(name);
        if (!Directory.Exists(agentDir))
        {
            Console.WriteLine($"[agent] Agent '{name}' not found.");
            return;
        }

        Directory.Delete(agentDir, true);
        Console.WriteLine($"[agent] Deleted agent '{name}'.");
    }

    public void List()
    {
        if (!Directory.Exists(_path.AgentsRoot))
        {
            Console.WriteLine("[agent] No agents found.");
            return;
        }

        var agents = Directory.GetDirectories(_path.AgentsRoot)
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .ToList();

        if (agents.Count == 0)
        {
            Console.WriteLine("[agent] No agents found.");
            return;
        }

        Console.WriteLine("[agent] Available agents:");
        foreach (var agent in agents)
        {
            Console.WriteLine($"  - {agent}");
        }
    }

    public void ShowConfig(string name)
    {
        var path = _path.GetAgentConfigJsonPath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[agent] Config for '{name}' not found.");
            return;
        }

        var config = AgentConfig.Load(path);
        Console.WriteLine($"Name:          {config.Name}");
        Console.WriteLine($"Model:         {config.ModelId}");
        Console.WriteLine($"Base URL:      {config.BaseUrl}");
        Console.WriteLine($"API Type:      {config.ApiType}");
        Console.WriteLine($"Context Win:   {config.ContextWindow}");
        Console.WriteLine($"Max Output:    {config.MaxOutputToken}");
        Console.WriteLine($"Temperature:   {config.Temperature}");
        Console.WriteLine($"Concurrency:   {config.MaxConcurrency}");
    }

    public void EditConfig(string name)
    {
        var path = _path.GetAgentConfigJsonPath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[agent] Config for '{name}' not found.");
            return;
        }

        Console.WriteLine($"[agent] Opening config for '{name}'...");
        OpenEditor(path);
    }

    public void EditIdentity(string name)
    {
        var path = _path.GetIdentityPath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[agent] Identity for '{name}' not found.");
            return;
        }

        Console.WriteLine($"[agent] Opening identity for '{name}'...");
        OpenEditor(path);
    }

    public void EditUser(string name)
    {
        var path = _path.GetUserPath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[agent] User profile for '{name}' not found.");
            return;
        }

        Console.WriteLine($"[agent] Opening user profile for '{name}'...");
        OpenEditor(path);
    }

    public bool Exists(string name) => Directory.Exists(_path.GetAgentPath(name));

    private static void OpenEditor(string path)
    {
        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrEmpty(editor))
        {
            System.Diagnostics.Process.Start(editor, $"\"{path}\"");
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        else
        {
            var editors = new[] { "nano", "vim", "vi" };
            foreach (var ed in editors)
            {
                try
                {
                    System.Diagnostics.Process.Start(ed, $"\"{path}\"");
                    return;
                }
                catch { }
            }
            Console.WriteLine($"[agent] Please set EDITOR environment variable or manually edit: {path}");
        }
    }
}
