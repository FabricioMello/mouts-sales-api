using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string EventName { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string eventName, string payload, DateTime occurredAt)
    {
        EventName = eventName;
        Payload = payload;
        OccurredAt = occurredAt;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
