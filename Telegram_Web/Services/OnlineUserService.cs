using System.Collections.Concurrent;
using System.Linq;

public class OnlineUserService
{
    // We store the Circuit ID and the User's Name
    private readonly ConcurrentDictionary<string, string> _onlineUsers = new();

    public void Add(string circuitId, string EmpID)
    {
        _onlineUsers.TryAdd(circuitId, EmpID);
        NotifyStateChanged();
    }

    public void Remove(string circuitId)
    {
        _onlineUsers.TryRemove(circuitId, out _);
        NotifyStateChanged();
    }

    public int GetOnlineUsersCount()
    {
        // We count distinct user names, as a user might have multiple tabs/circuits open
        return _onlineUsers.Values.Distinct().Count();
    }
    public IEnumerable<string> GetOnlineUsernames() // <-- This is the corrected method name.
    {
        return _onlineUsers.Values.Distinct().OrderBy(u => u);
    }

    public IEnumerable<string> GetOnlineUsers()
    {
        return _onlineUsers.Values.Distinct().OrderBy(u => u);
    }

    public bool IsUserOnline(string EmpID)
    {
        // We use StringComparer.OrdinalIgnoreCase for a case-insensitive check,
        // which is generally more robust for usernames.
        return _onlineUsers.Values.Contains(EmpID, StringComparer.OrdinalIgnoreCase);
    }

    // Event to notify components when the user list changes
    public event Action? OnChange;


    private void NotifyStateChanged() => OnChange?.Invoke();
}