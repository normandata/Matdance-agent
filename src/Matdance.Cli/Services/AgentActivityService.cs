using System.Collections.Concurrent;

namespace Matdance.Cli.Services;

public sealed class AgentActivityService
{
    private readonly ConcurrentDictionary<string, AgentActivityState> _states = new(StringComparer.OrdinalIgnoreCase);

    public IDisposable BeginUserTurn(string agent)
    {
        var state = GetState(agent);
        lock (state)
        {
            state.ActiveUserTurns++;
            state.LastUserActivity = DateTimeOffset.UtcNow;
            state.NextUserActivityCts.Cancel();
            state.NextUserActivityCts.Dispose();
            state.NextUserActivityCts = new CancellationTokenSource();
        }

        return new UserTurnLease(this, agent);
    }

    public bool HasActiveUserTurn(string agent)
    {
        if (!_states.TryGetValue(agent, out var state))
            return false;

        lock (state)
        {
            return state.ActiveUserTurns > 0;
        }
    }

    public int GetActiveUserTurnCount(string agent)
    {
        if (!_states.TryGetValue(agent, out var state))
            return 0;

        lock (state)
        {
            return state.ActiveUserTurns;
        }
    }

    public DateTimeOffset? GetLastUserActivity(string agent)
    {
        if (!_states.TryGetValue(agent, out var state))
            return null;

        lock (state)
        {
            return state.LastUserActivity;
        }
    }

    public CancellationToken NextUserActivityToken(string agent)
    {
        var state = GetState(agent);
        lock (state)
        {
            return state.NextUserActivityCts.Token;
        }
    }

    private void EndUserTurn(string agent)
    {
        if (!_states.TryGetValue(agent, out var state))
            return;

        lock (state)
        {
            state.ActiveUserTurns = Math.Max(0, state.ActiveUserTurns - 1);
            state.LastUserActivity = DateTimeOffset.UtcNow;
        }
    }

    public void RemoveAgent(string agent)
    {
        if (!_states.TryRemove(agent, out var state))
            return;

        lock (state)
        {
            try { state.NextUserActivityCts.Cancel(); } catch { }
            state.NextUserActivityCts.Dispose();
        }
    }

    private AgentActivityState GetState(string agent)
    {
        return _states.GetOrAdd(agent, _ => new AgentActivityState());
    }

    private sealed class UserTurnLease : IDisposable
    {
        private readonly AgentActivityService _owner;
        private readonly string _agent;
        private bool _disposed;

        public UserTurnLease(AgentActivityService owner, string agent)
        {
            _owner = owner;
            _agent = agent;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.EndUserTurn(_agent);
        }
    }

    private sealed class AgentActivityState
    {
        public int ActiveUserTurns { get; set; }
        public DateTimeOffset? LastUserActivity { get; set; }
        public CancellationTokenSource NextUserActivityCts { get; set; } = new();
    }
}
