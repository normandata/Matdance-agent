using System.Text;

namespace Matdance.Cli.Services;

public static class AtomicFile
{
    public static void WriteAllText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = BuildTempPath(path);
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            Commit(tempPath, path);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public static async Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = BuildTempPath(path);
        try
        {
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, ct);
            Commit(tempPath, path);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static string BuildTempPath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp";
        return string.IsNullOrWhiteSpace(directory) ? fileName : Path.Combine(directory, fileName);
    }

    private static void Commit(string tempPath, string path)
    {
        if (!File.Exists(path))
        {
            File.Move(tempPath, path);
            return;
        }

        try
        {
            File.Replace(tempPath, path, null);
        }
        catch
        {
            File.Delete(path);
            File.Move(tempPath, path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
