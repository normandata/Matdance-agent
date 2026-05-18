using System.Security.Cryptography;
using System.Text;
using Matdance.Cli.Models;
using Matdance.Cli.Services;

namespace Matdance.Cli.Core;

internal static class FileTraceLockService
{
    public const int MaxReadLocks = 3;
    public const int MaxWriteLocks = 3;
    public const int MaxReadLockLines = 2000;
    public const long MaxTraceFileBytes = 5_000_000;
    public const int DefaultReadLockLines = 240;
    public const int WriteLockRadius = 100;
    public const int MaxRenderedLockChars = 80_000;
    private const int MetadataTimeoutMs = 1_000;
    private const int TextReadTimeoutMs = 3_000;
    private static readonly TimeSpan TimeoutCooldown = TimeSpan.FromSeconds(30);

    public static string NewId(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, prefix.Length + 33)];
    }

    public static FileTraceRefreshResult Refresh(TracedFileInfo trace)
    {
        EnsureDefaults(trace);
        if (IsRecentTimeout(trace))
        {
            return new FileTraceRefreshResult(trace, trace.Content, trace.StartLine, trace.EndLine, trace.LineCount, trace.Status, trace.Message);
        }

        if (!TryGetExistingFileLength(trace.Path, out var exists, out var length, out var metadataError))
        {
            MarkUnavailable(trace, "metadata_timeout", metadataError ?? $"Timed out after {MetadataTimeoutMs}ms while inspecting file metadata.");
            return new FileTraceRefreshResult(trace, string.Empty, trace.StartLine, trace.EndLine, trace.LineCount, trace.Status, trace.Message);
        }

        if (!exists)
        {
            MarkUnavailable(trace, "missing", "File no longer exists.");
            return new FileTraceRefreshResult(trace, string.Empty, trace.StartLine, trace.EndLine, 0, "missing", trace.Message);
        }

        if (length > MaxTraceFileBytes)
        {
            MarkUnavailable(trace, "too_large", $"File is larger than {MaxTraceFileBytes} bytes. Use file_search with a narrower path or split the file before opening a live lock.");
            return new FileTraceRefreshResult(trace, string.Empty, trace.StartLine, trace.EndLine, 0, "too_large", trace.Message);
        }

        if (!TryReadAllText(trace.Path, out var text, out var readError))
        {
            MarkUnavailable(trace, "read_timeout", readError ?? $"Timed out after {TextReadTimeoutMs}ms while reading file text.");
            return new FileTraceRefreshResult(trace, string.Empty, trace.StartLine, trace.EndLine, trace.LineCount, trace.Status, trace.Message);
        }

        var lines = SplitLines(text);
        trace.LineCount = lines.Length;
        if (lines.Length == 0)
        {
            trace.StartLine = 1;
            trace.EndLine = 0;
            trace.CenterLine = 1;
            trace.Content = string.Empty;
            trace.Status = "fresh";
            trace.Message = null;
            trace.ContentHash = Sha256(text);
            trace.LastRead = UserTimeZoneService.Now();
            return new FileTraceRefreshResult(trace, string.Empty, 1, 0, 0, "fresh", null);
        }

        var maxLines = ClampMaxLines(trace.MaxLines);
        var requestedSpan = trace.EndLine >= trace.StartLine ? trace.EndLine - trace.StartLine + 1 : maxLines;
        var span = Math.Clamp(requestedSpan <= 0 ? maxLines : requestedSpan, 1, maxLines);
        var status = "fresh";
        string? message = null;
        int start;
        int end;

        if (string.Equals(trace.Mode, "semantic", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(trace.Anchor))
        {
            var anchorLine = FindLineContaining(lines, trace.Anchor!);
            if (anchorLine < 1 && !string.IsNullOrWhiteSpace(trace.AnchorText))
            {
                anchorLine = FindLineContaining(lines, trace.AnchorText!);
            }

            if (anchorLine >= 1)
            {
                (start, end) = CenterWindow(lines.Length, anchorLine, span);
                trace.CenterLine = anchorLine;
            }
            else
            {
                status = "stale";
                message = "Semantic anchor was not found in the current file. This lock is shown as a physical fallback; do not write based on it without opening a fresh trace.";
                (start, end) = ClipRange(lines.Length, trace.StartLine, trace.EndLine, maxLines);
            }
        }
        else if (string.Equals(trace.Mode, "full", StringComparison.OrdinalIgnoreCase))
        {
            (start, end) = ClipRange(lines.Length, 1, Math.Min(lines.Length, maxLines), maxLines);
            trace.CenterLine = start;
        }
        else
        {
            (start, end) = ClipRange(lines.Length, trace.StartLine, trace.EndLine, maxLines);
            trace.CenterLine = Math.Clamp(trace.CenterLine <= 0 ? start : trace.CenterLine, start, Math.Max(start, end));
        }

        var content = RenderLines(lines, start, end);
        trace.StartLine = start;
        trace.EndLine = end;
        trace.Status = status;
        trace.Message = message;
        trace.Content = content;
        trace.ContentHash = Sha256(string.Join('\n', lines.Skip(Math.Max(0, start - 1)).Take(Math.Max(0, end - start + 1))));
        trace.LastRead = UserTimeZoneService.Now();

        return new FileTraceRefreshResult(trace, content, start, end, lines.Length, status, message);
    }

    public static TracedFileInfo CreateReadLock(
        string fullPath,
        int? startLine,
        int? endLine,
        string? anchor,
        string? mode,
        int? maxLines)
    {
        if (!TryGetExistingFileLength(fullPath, out var exists, out var length, out var metadataError))
        {
            var unavailable = new TracedFileInfo
            {
                Id = NewId("read"),
                Kind = "read",
                Mode = "physical",
                Path = fullPath,
                StartLine = Math.Max(1, startLine ?? 1),
                EndLine = Math.Max(1, endLine ?? startLine ?? 1),
                CenterLine = Math.Max(1, startLine ?? 1),
                MaxLines = ClampMaxLines(maxLines ?? DefaultReadLockLines),
                Status = "metadata_timeout",
                Message = metadataError ?? $"Timed out after {MetadataTimeoutMs}ms while inspecting file metadata.",
                LastRead = UserTimeZoneService.Now()
            };
            return unavailable;
        }

        if (exists && length > MaxTraceFileBytes)
        {
            var oversized = new TracedFileInfo
            {
                Id = NewId("read"),
                Kind = "read",
                Mode = "physical",
                Path = fullPath,
                StartLine = Math.Max(1, startLine ?? 1),
                EndLine = Math.Max(1, endLine ?? startLine ?? 1),
                CenterLine = Math.Max(1, startLine ?? 1),
                MaxLines = ClampMaxLines(maxLines ?? DefaultReadLockLines),
                Status = "too_large",
                Message = $"File is larger than {MaxTraceFileBytes} bytes. Use file_search with a narrower path or split the file before opening a live lock.",
                LastRead = UserTimeZoneService.Now()
            };
            return oversized;
        }

        var text = string.Empty;
        if (exists && !TryReadAllText(fullPath, out text, out var readError))
        {
            var unavailable = new TracedFileInfo
            {
                Id = NewId("read"),
                Kind = "read",
                Mode = "physical",
                Path = fullPath,
                StartLine = Math.Max(1, startLine ?? 1),
                EndLine = Math.Max(1, endLine ?? startLine ?? 1),
                CenterLine = Math.Max(1, startLine ?? 1),
                MaxLines = ClampMaxLines(maxLines ?? DefaultReadLockLines),
                Status = "read_timeout",
                Message = readError ?? $"Timed out after {TextReadTimeoutMs}ms while reading file text.",
                LastRead = UserTimeZoneService.Now()
            };
            return unavailable;
        }
        var lines = SplitLines(text);
        var lineCount = Math.Max(0, lines.Length);
        var max = ClampMaxLines(maxLines ?? DefaultReadLockLines);
        var actualMode = string.IsNullOrWhiteSpace(mode)
            ? string.IsNullOrWhiteSpace(anchor) ? "physical" : "semantic"
            : mode!.Trim().ToLowerInvariant();

        int start;
        int end;
        int center;
        string? anchorText = null;
        if (lineCount == 0)
        {
            start = 1;
            end = 0;
            center = 1;
        }
        else if (!string.IsNullOrWhiteSpace(anchor))
        {
            var anchorLine = FindLineContaining(lines, anchor!);
            center = anchorLine >= 1 ? anchorLine : Math.Clamp(startLine ?? 1, 1, lineCount);
            (start, end) = CenterWindow(lineCount, center, max);
            anchorText = GetFirstNonEmpty(lines, start, end);
        }
        else if (startLine.HasValue || endLine.HasValue)
        {
            (start, end) = ClipRange(lineCount, startLine ?? 1, endLine ?? startLine ?? Math.Min(lineCount, max), max);
            center = Math.Clamp(startLine ?? start, start, Math.Max(start, end));
            anchorText = GetFirstNonEmpty(lines, start, end);
        }
        else
        {
            (start, end) = ClipRange(lineCount, 1, Math.Min(lineCount, max), max);
            center = 1;
            anchorText = GetFirstNonEmpty(lines, start, end);
        }

        var trace = new TracedFileInfo
        {
            Id = NewId("read"),
            Kind = "read",
            Mode = actualMode is "semantic" or "full" ? actualMode : "physical",
            Path = fullPath,
            Anchor = string.IsNullOrWhiteSpace(anchor) ? anchorText : anchor,
            AnchorText = anchorText,
            StartLine = start,
            EndLine = end,
            CenterLine = center,
            MaxLines = max,
            LineCount = lineCount,
            LastRead = UserTimeZoneService.Now()
        };

        Refresh(trace);
        return trace;
    }

    public static TracedFileInfo CreateWriteLock(string fullPath, int centerLine, int totalLines)
    {
        var (start, end) = WriteWindow(totalLines, centerLine);
        var trace = new TracedFileInfo
        {
            Id = NewId("write"),
            Kind = "write",
            Mode = "physical",
            Path = fullPath,
            StartLine = start,
            EndLine = end,
            CenterLine = Math.Clamp(centerLine, 1, Math.Max(1, totalLines)),
            MaxLines = Math.Max(1, end - start + 1),
            LineCount = totalLines,
            LastRead = UserTimeZoneService.Now()
        };
        Refresh(trace);
        return trace;
    }

    public static (int Start, int End) WriteWindow(int totalLines, int centerLine)
    {
        if (totalLines <= 0)
            return (1, 0);

        var desired = WriteLockRadius * 2 + 1;
        if (totalLines <= desired)
            return (1, totalLines);

        var center = Math.Clamp(centerLine, 1, totalLines);
        var start = center - WriteLockRadius;
        var end = center + WriteLockRadius;
        if (start < 1)
        {
            end = Math.Min(totalLines, end + (1 - start));
            start = 1;
        }
        if (end > totalLines)
        {
            start = Math.Max(1, start - (end - totalLines));
            end = totalLines;
        }
        return (start, end);
    }

    public static string DescribeLock(TracedFileInfo trace, bool includeContent = true)
    {
        var refreshed = Refresh(trace);
        var sb = new StringBuilder();
        sb.AppendLine($"{LockLabel(trace)}: {trace.Path}");
        sb.AppendLine($"Status: {refreshed.Status}");
        sb.AppendLine($"Mode: {trace.Mode}");
        sb.AppendLine($"Range: L{refreshed.StartLine}-L{refreshed.EndLine} of {refreshed.LineCount}");
        sb.AppendLine($"Hash: {trace.ContentHash}");
        if (!string.IsNullOrWhiteSpace(refreshed.Message))
            sb.AppendLine($"Note: {refreshed.Message}");
        if (includeContent)
        {
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(refreshed.Content);
            sb.AppendLine("```");
        }
        return sb.ToString().TrimEnd();
    }

    public static string LockLabel(TracedFileInfo trace)
    {
        EnsureDefaults(trace);
        var prefix = string.Equals(trace.Kind, "write", StringComparison.OrdinalIgnoreCase) ? "Write Lock" : "Read Lock";
        return $"{prefix} {trace.Id}";
    }

    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }

    private static void EnsureDefaults(TracedFileInfo trace)
    {
        if (string.IsNullOrWhiteSpace(trace.Id))
            trace.Id = NewId(string.Equals(trace.Kind, "write", StringComparison.OrdinalIgnoreCase) ? "write" : "read");
        if (string.IsNullOrWhiteSpace(trace.Kind))
            trace.Kind = "read";
        if (string.IsNullOrWhiteSpace(trace.Mode))
            trace.Mode = "physical";
        trace.MaxLines = ClampMaxLines(trace.MaxLines <= 0 ? MaxReadLockLines : trace.MaxLines);
        if (trace.StartLine <= 0)
            trace.StartLine = 1;
        if (trace.EndLine <= 0)
            trace.EndLine = trace.StartLine;
        if (trace.CenterLine <= 0)
            trace.CenterLine = trace.StartLine;
    }

    private static bool IsRecentTimeout(TracedFileInfo trace)
    {
        if (trace.Status is not ("metadata_timeout" or "read_timeout"))
            return false;
        return UserTimeZoneService.Now() - trace.LastRead < TimeoutCooldown;
    }

    private static void MarkUnavailable(TracedFileInfo trace, string status, string message)
    {
        trace.Status = status;
        trace.Message = message;
        trace.Content = string.Empty;
        trace.LineCount = 0;
        trace.ContentHash = string.Empty;
        trace.LastRead = UserTimeZoneService.Now();
    }

    private static bool TryGetExistingFileLength(string path, out bool exists, out long length, out string? error)
    {
        try
        {
            exists = File.Exists(path);
            length = exists ? new FileInfo(path).Length : 0;
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            exists = false;
            length = 0;
            error = ex.Message;
            return false;
        }
    }

    private static bool TryReadAllText(string path, out string text, out string? error)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TextReadTimeoutMs));
            text = File.ReadAllTextAsync(path, cts.Token)
                .WaitAsync(TimeSpan.FromMilliseconds(TextReadTimeoutMs))
                .GetAwaiter()
                .GetResult();
            error = null;
            return true;
        }
        catch (TimeoutException)
        {
            text = string.Empty;
            error = $"Timed out after {TextReadTimeoutMs}ms while reading file text. Close this lock or open a narrower/local trace.";
            return false;
        }
        catch (OperationCanceledException)
        {
            text = string.Empty;
            error = $"Timed out after {TextReadTimeoutMs}ms while reading file text. Close this lock or open a narrower/local trace.";
            return false;
        }
        catch (Exception ex)
        {
            text = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static int ClampMaxLines(int value)
    {
        return Math.Clamp(value <= 0 ? DefaultReadLockLines : value, 1, MaxReadLockLines);
    }

    private static (int Start, int End) ClipRange(int totalLines, int startLine, int endLine, int maxLines)
    {
        if (totalLines <= 0)
            return (1, 0);
        var start = Math.Clamp(startLine <= 0 ? 1 : startLine, 1, totalLines);
        var end = Math.Clamp(endLine <= 0 ? start : endLine, 1, totalLines);
        if (end < start)
            (start, end) = (end, start);
        if (end - start + 1 > maxLines)
            end = Math.Min(totalLines, start + maxLines - 1);
        return (start, end);
    }

    private static (int Start, int End) CenterWindow(int totalLines, int centerLine, int span)
    {
        if (totalLines <= 0)
            return (1, 0);
        var desired = Math.Clamp(span, 1, MaxReadLockLines);
        if (totalLines <= desired)
            return (1, totalLines);
        var center = Math.Clamp(centerLine, 1, totalLines);
        var before = desired / 2;
        var after = desired - before - 1;
        var start = center - before;
        var end = center + after;
        if (start < 1)
        {
            end = Math.Min(totalLines, end + (1 - start));
            start = 1;
        }
        if (end > totalLines)
        {
            start = Math.Max(1, start - (end - totalLines));
            end = totalLines;
        }
        return (start, end);
    }

    private static int FindLineContaining(string[] lines, string needle)
    {
        if (string.IsNullOrWhiteSpace(needle))
            return -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                return i + 1;
        }
        return -1;
    }

    private static string? GetFirstNonEmpty(string[] lines, int start, int end)
    {
        for (var i = Math.Max(0, start - 1); i <= Math.Min(lines.Length - 1, end - 1); i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrWhiteSpace(line))
                return line.Length > 160 ? line[..160] : line;
        }
        return null;
    }

    private static string RenderLines(string[] lines, int start, int end)
    {
        if (lines.Length == 0 || end < start)
            return string.Empty;

        var width = Math.Max(1, end.ToString().Length);
        var sb = new StringBuilder();
        for (var lineNo = start; lineNo <= end && lineNo <= lines.Length; lineNo++)
        {
            sb.Append(lineNo.ToString().PadLeft(width));
            sb.Append(" | ");
            sb.AppendLine(lines[lineNo - 1]);
            if (sb.Length > MaxRenderedLockChars)
            {
                sb.AppendLine($"...[lock content truncated at {MaxRenderedLockChars} chars; open a narrower trace before editing]");
                break;
            }
        }
        return sb.ToString().TrimEnd();
    }
}

internal sealed record FileTraceRefreshResult(
    TracedFileInfo Trace,
    string Content,
    int StartLine,
    int EndLine,
    int LineCount,
    string Status,
    string? Message);
