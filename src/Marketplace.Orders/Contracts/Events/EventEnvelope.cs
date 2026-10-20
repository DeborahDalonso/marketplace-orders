using System.Text.Json;

namespace Marketplace.Orders.Contracts.Events;

// Envelope do CONTRATOS 4.1. O mesmo record serve para produzir e consumir.
public record EventEnvelope<T>(
    string EventId,
    string Type,
    int Version,
    DateTime OccurredAt,
    string Producer,
    T Data)
{
    public const string ProducerName = "marketplace-orders";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static EventEnvelope<T> Create(string type, T data) =>
        new(Domain.Ulid.New("evt_"), type, 1, DateTime.UtcNow, ProducerName, data);

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}
