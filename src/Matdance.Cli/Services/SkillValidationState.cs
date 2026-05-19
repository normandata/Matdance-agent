using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Matdance.Cli.Services;

public static class SkillValidationState
{
    private const string FingerprintMarker = "matdance-validation-fingerprint:";
    public const string CurrentMaintenanceMode = "validation-and-repair-v1";

    public static string GetReportPath(string skillDir) => Path.Combine(skillDir, "validation-report.md");

    public static string GetImportReportPath(string skillDir) => Path.Combine(skillDir, "import-report.md");

    public static void DeleteReport(string skillDir)
    {
        var reportPath = GetReportPath(skillDir);
        if (File.Exists(reportPath))
            File.Delete(reportPath);
    }

    public static bool EnsureReportCurrent(string skillDir)
    {
        var reportPath = GetReportPath(skillDir);
        if (!File.Exists(reportPath))
            return false;

        var report = File.ReadAllText(reportPath);
        var match = Regex.Match(report, @"matdance-validation-fingerprint:\s*([a-fA-F0-9]{64})");
        if (!match.Success)
        {
            return false;
        }

        var expected = ComputeFingerprint(skillDir);
        if (string.Equals(match.Groups[1].Value, expected, StringComparison.OrdinalIgnoreCase))
            return true;

        File.Delete(reportPath);
        return false;
    }

    public static string AddFingerprint(string skillDir, string report)
    {
        var cleaned = Regex.Replace(report.TrimEnd(), @"\r?\n?<!--\s*matdance-validation-fingerprint:\s*[a-fA-F0-9]{64}\s*-->\s*$", string.Empty);
        return cleaned.TrimEnd() + Environment.NewLine + Environment.NewLine + $"<!-- {FingerprintMarker} {ComputeFingerprint(skillDir)} -->" + Environment.NewLine;
    }

    public static string GetValidationStatusLine(string skillDir)
    {
        if (!EnsureReportCurrent(skillDir))
            return "validation: unverified";

        var reportPath = GetReportPath(skillDir);
        if (!File.Exists(reportPath))
            return "validation: unverified";

        var report = File.ReadAllText(reportPath);
        var status = ExtractMetadata(report, "Status") ?? "unknown";
        var score = ExtractMetadata(report, "Score");
        var summary = ExtractSection(report, "Summary");
        var statusText = string.IsNullOrWhiteSpace(score)
            ? $"validation: {status}"
            : $"validation: {status} {score}";

        if (!string.IsNullOrWhiteSpace(summary))
            statusText += " - " + OneLine(summary, 180);

        return statusText;
    }

    public static string BuildSkillReportContext(string skillDir, bool detailed)
    {
        var sb = new StringBuilder();

        if (EnsureReportCurrent(skillDir) && File.Exists(GetReportPath(skillDir)))
        {
            sb.AppendLine("### Validation Report");
            sb.AppendLine(SummarizeValidationReport(File.ReadAllText(GetReportPath(skillDir)), detailed));
        }
        else
        {
            sb.AppendLine("### Validation Report");
            sb.AppendLine("- Status: unverified");
            sb.AppendLine("- Note: no current validation report exists. Treat this skill as pending idle validation.");
        }

        var importReportPath = GetImportReportPath(skillDir);
        if (File.Exists(importReportPath))
        {
            sb.AppendLine();
            sb.AppendLine("### Import Report");
            sb.AppendLine(SummarizeImportReport(File.ReadAllText(importReportPath), detailed));
        }

        return sb.ToString().TrimEnd();
    }

    public static bool NeedsValidation(string skillDir)
    {
        return NeedsAutomaticValidation(skillDir);
    }

    public static bool NeedsAutomaticValidation(string skillDir)
    {
        return !EnsureReportCurrent(skillDir) || !File.Exists(GetReportPath(skillDir));
    }

    public static string ComputeFingerprint(string skillDir)
    {
        using var sha = SHA256.Create();
        var files = Directory.Exists(skillDir)
            ? Directory.EnumerateFiles(skillDir, "*", SearchOption.AllDirectories)
                .Where(file => ShouldFingerprint(skillDir, file))
                .OrderBy(file => Path.GetRelativePath(skillDir, file), StringComparer.OrdinalIgnoreCase)
                .ToList()
            : new List<string>();

        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(skillDir, file).Replace(Path.DirectorySeparatorChar, '/');
            var header = Encoding.UTF8.GetBytes(relative + "\n");
            sha.TransformBlock(header, 0, header.Length, null, 0);
            var bytes = File.ReadAllBytes(file);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            var separator = Encoding.UTF8.GetBytes("\n---\n");
            sha.TransformBlock(separator, 0, separator.Length, null, 0);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
    }

    private static bool ShouldFingerprint(string skillDir, string file)
    {
        var name = Path.GetFileName(file);
        if (name.Equals("validation-report.md", StringComparison.OrdinalIgnoreCase))
            return false;
        if (name.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
            return false;

        var relative = Path.GetRelativePath(skillDir, file).Replace('\\', '/');
        if (relative.Contains("/__pycache__/", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("__pycache__/", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string SummarizeValidationReport(string report, bool detailed)
    {
        var sb = new StringBuilder();
        AppendMetadata(sb, report, "Status");
        AppendMetadata(sb, report, "Score");
        AppendMetadata(sb, report, "Checked At");
        AppendMetadata(sb, report, "Maintenance Mode");
        AppendSection(sb, report, "Summary", maxItems: 1, maxChars: detailed ? 700 : 260);
        AppendSection(sb, report, "Applied Repairs", maxItems: detailed ? 8 : 3, maxChars: 240);
        AppendSection(sb, report, "Findings", maxItems: detailed ? 8 : 3, maxChars: 240);
        AppendSection(sb, report, "Suggested Changes", maxItems: detailed ? 8 : 3, maxChars: 240);
        return sb.ToString().TrimEnd();
    }

    private static string SummarizeImportReport(string report, bool detailed)
    {
        var sb = new StringBuilder();
        AppendMetadata(sb, report, "Decision");
        AppendMetadata(sb, report, "Checked At");
        AppendSection(sb, report, "Summary", maxItems: 1, maxChars: detailed ? 700 : 260);
        AppendSection(sb, report, "Unsupported Assumptions", maxItems: detailed ? 6 : 2, maxChars: 240);
        AppendSection(sb, report, "Validation Notes", maxItems: detailed ? 6 : 2, maxChars: 240);
        AppendSection(sb, report, "Safety Findings", maxItems: detailed ? 6 : 2, maxChars: 240);
        AppendSection(sb, report, "Skipped Source Files", maxItems: detailed ? 6 : 2, maxChars: 240);
        return sb.ToString().TrimEnd();
    }

    private static void AppendMetadata(StringBuilder sb, string report, string name)
    {
        var value = ExtractMetadata(report, name);
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"- {name}: {value}");
    }

    private static string? ExtractMetadata(string report, string name)
    {
        var match = Regex.Match(report, @"(?m)^-\s*" + Regex.Escape(name) + @":\s*(.+?)\s*$");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static void AppendSection(StringBuilder sb, string report, string title, int maxItems, int maxChars)
    {
        var section = ExtractSection(report, title);
        if (string.IsNullOrWhiteSpace(section))
            return;

        sb.AppendLine($"- {title}:");
        var lines = SectionLines(section, maxItems, maxChars).ToList();
        foreach (var line in lines)
            sb.AppendLine($"  - {line}");
    }

    private static string ExtractSection(string report, string title)
    {
        var match = Regex.Match(
            report,
            @"(?ms)^##\s+" + Regex.Escape(title) + @"\s*\r?\n(.*?)(?=^##\s+|\z)");
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static IEnumerable<string> SectionLines(string section, int maxItems, int maxChars)
    {
        var lines = section
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => Regex.Replace(line, @"^[-*]\s+", string.Empty).Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(maxItems);

        foreach (var line in lines)
            yield return OneLine(line, maxChars);
    }

    private static string OneLine(string value, int maxChars)
    {
        var cleaned = Regex.Replace(value.Trim(), @"\s+", " ");
        return cleaned.Length <= maxChars ? cleaned : cleaned[..maxChars] + "...";
    }
}
