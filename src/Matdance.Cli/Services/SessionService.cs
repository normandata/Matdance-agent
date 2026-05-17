using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class SessionService
{
    private readonly PathService _path;

    public SessionService(PathService path)
    {
        _path = path;
    }

    public void Create(string agentName, string? sessionId = null)
    {
        var sessionsDir = _path.GetSessionsPath(agentName);
        if (!Directory.Exists(sessionsDir))
        {
            Console.WriteLine($"[session] Agent '{agentName}' does not exist.");
            return;
        }

        var id = _path.NormalizeSessionId(sessionId ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        var filePath = _path.GetSessionJsonPath(agentName, id);

        var session = new SessionData
        {
            SessionId = id,
            CreateAt = UserTimeZoneService.Now(),
            LastActivity = UserTimeZoneService.Now()
        };
        session.Save(filePath);

        Console.WriteLine($"[session] Created session '{id}' for agent '{agentName}'.");
    }

    public void List(string agentName)
    {
        var sessionsDir = _path.GetSessionsPath(agentName);
        if (!Directory.Exists(sessionsDir))
        {
            Console.WriteLine($"[session] Agent '{agentName}' does not exist.");
            return;
        }

        var files = Directory.GetFiles(sessionsDir, "*.json");
        if (files.Length == 0)
        {
            Console.WriteLine($"[session] No sessions found for '{agentName}'.");
            return;
        }

        Console.WriteLine($"[session] Sessions for '{agentName}':");
        foreach (var file in files.OrderBy(f => f))
        {
            var session = SessionData.Load(file);
            Console.WriteLine($"  - {session.SessionId} | msgs:{session.TotalMessages} | tools:{session.ToolMessagesCount} | tokens:{session.Tokens} | created:{session.CreateAt:yyyy-MM-dd HH:mm}");
        }
    }

    public void Show(string agentName, string sessionId)
    {
        var filePath = _path.GetSessionJsonPath(agentName, sessionId);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[session] Session '{sessionId}' not found.");
            return;
        }

        var session = SessionData.Load(filePath);
        Console.WriteLine($"Session ID:       {session.SessionId}");
        Console.WriteLine($"Created At:       {session.CreateAt}");
        Console.WriteLine($"Context Usage:    {session.ContextUsage}%");
        Console.WriteLine($"Total Messages:   {session.TotalMessages}");
        Console.WriteLine($"Tool Messages:    {session.ToolMessagesCount}");
        Console.WriteLine($"Tokens:           {session.Tokens}");
        Console.WriteLine($"Tasks:            {session.Tasks.Count}");
        Console.WriteLine($"Issues:           {session.Issues.Count}");
    }

    public void Delete(string agentName, string sessionId)
    {
        var sessionsDir = _path.GetSessionsPath(agentName);
        var safeSessionId = _path.NormalizeSessionId(sessionId);
        var filePath = _path.GetSessionJsonPath(agentName, safeSessionId);
        var statePath = _path.GetSessionStateJsonPath(agentName, safeSessionId);

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[session] Session '{sessionId}' not found.");
            return;
        }

        File.Delete(filePath);
        if (File.Exists(statePath))
        {
            File.Delete(statePath);
        }
        Console.WriteLine($"[session] Deleted session '{sessionId}'.");
    }
}
