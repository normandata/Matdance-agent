namespace Matdance.Cli.Services;

public class MemoryService
{
    private readonly PathService _path;

    public MemoryService(PathService path)
    {
        _path = path;
    }

    public void ShowHot(string agentName)
    {
        var path = _path.GetHotMemoryPath(agentName);
        ShowFile(path, "hot memory");
    }

    public void EditHot(string agentName)
    {
        var path = _path.GetHotMemoryPath(agentName);
        OpenEditor(path);
    }

    public void ListLongTerm(string agentName)
    {
        var dir = _path.GetLongTermMemoryPath(agentName);
        if (!Directory.Exists(dir))
        {
            Console.WriteLine("[memory] Long term memory directory not found.");
            return;
        }

        var files = Directory.GetFiles(dir, "*.md").Select(Path.GetFileName).OrderBy(f => f).ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("[memory] No long term memory files found.");
            return;
        }

        Console.WriteLine("[memory] Long term memory files:");
        foreach (var f in files)
        {
            Console.WriteLine($"  - {f}");
        }
    }

    public void ShowLongTerm(string agentName, string date)
    {
        var path = Path.Combine(_path.GetLongTermMemoryPath(agentName), $"{date}.md");
        ShowFile(path, $"long term memory for {date}");
    }

    public void EditLongTerm(string agentName, string date)
    {
        var dir = _path.GetLongTermMemoryPath(agentName);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{date}.md");
        OpenEditor(path);
    }

    public void ShowCore(string agentName)
    {
        var path = _path.GetCoreMemoryPath(agentName);
        ShowFile(path, "core memory");
    }

    public void EditCore(string agentName)
    {
        var path = _path.GetCoreMemoryPath(agentName);
        OpenEditor(path);
    }

    public void ShowVector(string agentName)
    {
        new VectorMemoryService(_path).Refresh(agentName);
        var path = _path.GetVectorMemoryPath(agentName);
        ShowFile(path, "vector memory base");
    }

    private static void ShowFile(string path, string label)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[memory] {label} not found at {path}");
            return;
        }

        Console.WriteLine($"--- {label} ---");
        Console.WriteLine(File.ReadAllText(path));
    }

    private static void OpenEditor(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "");
        }

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
            Console.WriteLine($"[memory] Please set EDITOR environment variable or manually edit: {path}");
        }
    }
}
