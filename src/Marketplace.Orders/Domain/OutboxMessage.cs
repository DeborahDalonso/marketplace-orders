namespace Marketplace.Orders.Domain;

public class OutboxMessage
{
    public long Id { get; private set; }

    public string EventId { get; private set; } = null!;

    public string Type { get; private set; } = null!;

    public string AggregateId { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string eventId, string type, string aggregateId, string payload)
    {
        EventId = eventId;
        Type = type;
        AggregateId = aggregateId;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkPublished()
    {
        PublishedAt = DateTime.UtcNow;
    }
}
