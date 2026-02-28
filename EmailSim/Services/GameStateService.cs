using System.Collections.Concurrent;

public class GameStateService
{
    private int _emailCount;
    private int _agentCount;
    private int _maxLimit = 10;
    private readonly ConcurrentQueue<EmailModel> _pendingEmails = new();
    private readonly object _countLock = new();

    public int EmailCount => _emailCount;
    public int AgentCount => _agentCount;
    public int TotalCount => _emailCount + _agentCount;
    public int MaxLimit { get => _maxLimit; set => _maxLimit = value; }

    public void AddEmail(EmailModel email)
    {
        _pendingEmails.Enqueue(email);
    }

    public List<EmailModel> DequeuePendingEmails()
    {
        var result = new List<EmailModel>();
        while (_pendingEmails.TryDequeue(out var email))
        {
            result.Add(email);
        }
        return result;
    }

    public void IncrementEmailCount()
    {
        lock (_countLock)
        {
            _emailCount++;
        }
    }

    public void IncrementAgentCount()
    {
        lock (_countLock)
        {
            _agentCount++;
        }
    }

    public void ResetCounts()
    {
        lock (_countLock)
        {
            _emailCount = 0;
            _agentCount = 0;
        }
    }
}
