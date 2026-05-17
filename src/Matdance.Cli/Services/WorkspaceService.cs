namespace Matdance.Cli.Services;

public class WorkspaceService
{
    private readonly PathService _path;

    public WorkspaceService(PathService path)
    {
        _path = path;
    }

    public void Open(string agentName)
    {
        var path = _path.GetWorkspacePath(agentName);
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"[workspace] Workspace for '{agentName}' not found.");
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            System.Diagnostics.Process.Start("open", $"\"{path}\"");
        }
        else
        {
            System.Diagnostics.Process.Start("xdg-open", $"\"{path}\"");
        }

        Console.WriteLine($"[workspace] Opened workspace for '{agentName}': {path}");
    }

    public void Tree(string agentName)
    {
        var path = _path.GetWorkspacePath(agentName);
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"[workspace] Workspace for '{agentName}' not found.");
            return;
        }

        Console.WriteLine($"[workspace] {agentName}/workspace");
        PrintTree(path, "");
    }

    private static void PrintTree(string directory, string prefix)
    {
        var files = Directory.GetFiles(directory).OrderBy(f => f).ToArray();
        var dirs = Directory.GetDirectories(directory).OrderBy(d => d).ToArray();

        for (int i = 0; i < dirs.Length; i++)
        {
            var isLast = i == dirs.Length - 1 && files.Length == 0;
            var name = Path.GetFileName(dirs[i]);
            Console.WriteLine($"{prefix}{(isLast ? "└── " : "├── ")}{name}/");
            PrintTree(dirs[i], prefix + (isLast ? "    " : "│   "));
        }

        for (int i = 0; i < files.Length; i++)
        {
            var isLast = i == files.Length - 1;
            var name = Path.GetFileName(files[i]);
            Console.WriteLine($"{prefix}{(isLast ? "└── " : "├── ")}{name}");
        }
    }
}
