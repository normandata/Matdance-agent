using System.Collections.Concurrent;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public static class SessionHostNoticeHub
{
    private static readonly ConcurrentDictionary<string, int> ActiveSessions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<ChatMessage>> Pending = new(StringComparer.OrdinalIgnoreCase);

    public static IDisposable BeginActive(string agent, string session)
    {
        var key = Key(agent, session);
        ActiveSessions.AddOrUpdate(key, 1, (_, count) => count + 1);
        return new ActiveLease(key);
    }

    public static bool PublishIfActive(string agent, string session, ChatMessage notice)
    {
        var key = Key(agent, session);
        if (!ActiveSessions.TryGetValue(key, out var count) || count <= 0)
            return false;

        Pending.GetOrAdd(key, _ => new ConcurrentQueue<ChatMessage>()).Enqueue(notice);
        return true;
    }

    public static List<ChatMessage> Drain(string agent, string session)
    {
        var key = Key(agent, session);
        if (!Pending.TryGetValue(key, out var queue))
            return new List<ChatMessage>();

        var result = new List<ChatMessage>();
        while (queue.TryDequeue(out var item))
            result.Add(item);

        if (queue.IsEmpty)
            Pending.TryRemove(key, out _);

        return result;
    }

    private static string Key(string agent, string session)
        => (agent ?? string.Empty).Trim().ToLowerInvariant() + "\u001f" + (session ?? string.Empty).Trim().ToLowerInvariant();

    private sealed class ActiveLease : IDisposable
    {
        private readonly string _key;
        private bool _disposed;

        public ActiveLease(string key)
        {
            _key = key;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ActiveSessions.AddOrUpdate(_key, 0, (_, count) => Math.Max(0, count - 1));
        }
    }
}
