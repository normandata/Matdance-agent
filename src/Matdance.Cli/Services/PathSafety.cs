namespace Matdance.Cli.Services;

public static class PathSafety
{
    public static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string NormalizeSeparators(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    public static bool ContainsParentTraversal(string path)
    {
        return SplitPathSegments(path).Any(part => part == "..");
    }

    public static string NormalizeFileNameSegment(string? value, string label)
    {
        var segment = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(segment))
            throw new InvalidOperationException($"{label} is required.");

        if (segment is "." or "..")
            throw new InvalidOperationException($"{label} is invalid.");

        if (Path.IsPathRooted(segment))
            throw new InvalidOperationException($"{label} cannot be an absolute path.");

        if (segment.Contains('/') || segment.Contains('\\'))
            throw new InvalidOperationException($"{label} cannot contain path separators.");

        if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            throw new InvalidOperationException($"{label} contains invalid characters.");

        if (ContainsParentTraversal(segment))
            throw new InvalidOperationException($"{label} cannot contain parent traversal.");

        return segment;
    }

    public static bool StartsWithSegment(string path, string segment)
    {
        var first = SplitPathSegments(path).FirstOrDefault();
        return first != null && string.Equals(first, segment, PathComparison);
    }

    public static bool IsUnderAnyRoot(string path, IEnumerable<string> roots) =>
        roots.Any(root => IsUnderRoot(path, root));

    public static bool IsUnderRoot(string path, string root)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
            return false;

        var fullPath = NormalizeFullPath(path);
        var fullRoot = NormalizeFullPath(root);

        if (fullPath.Equals(fullRoot, PathComparison))
            return true;

        return fullPath.StartsWith(EnsureTrailingSeparator(fullRoot), PathComparison);
    }

    private static string NormalizeFullPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.IsNullOrEmpty(trimmed))
            return trimmed;

        return Path.GetPathRoot(fullPath) ?? fullPath;
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static string[] SplitPathSegments(string path)
    {
        var normalized = NormalizeSeparators(path);
        return normalized.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
    }
}
