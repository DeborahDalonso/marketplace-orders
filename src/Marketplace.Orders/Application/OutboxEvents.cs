using Marketplace.Orders.Contracts.Events;
using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Application;

public static class OutboxEvents
{
    public static OutboxMessage Create<T>(string type, string aggregateId, T data)
    {
        var envelope = EventEnvelope<T>.Create(type, data);

        return new OutboxMessage(envelope.EventId, type, aggregateId, envelope.ToJson());
    }

    public static OutboxMessage OrderCancelled(Order order, string reason) =>
        Create("order.cancelled", order.Id, new OrderCancelledData(order.Id, order.BuyerId, reason));
}
