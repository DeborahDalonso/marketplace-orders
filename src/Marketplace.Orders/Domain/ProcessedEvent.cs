namespace Marketplace.Orders.Domain;

public class ProcessedEvent(string eventId, string type)
{
    public string EventId { get; private set; } = eventId;

    public string Type { get; private set; } = type;

    public DateTime ProcessedAt { get; private set; } = DateTime.UtcNow;
}
