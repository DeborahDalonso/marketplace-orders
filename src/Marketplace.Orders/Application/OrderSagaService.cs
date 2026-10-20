using Marketplace.Orders.Contracts.Events;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Orders.Application;

// Lado consumidor da saga: reage a stock.reserved e stock.rejected vindos do catalog.
// Cada handler é idempotente pelo eventId (tabela processed_events) e fecha tudo
// em um SaveChanges só: status, outbox e marcação do evento.
public class OrderSagaService(OrdersDbContext db, PaymentSimulator payment, ILogger<OrderSagaService> logger)
{
    public async Task HandleStockReservedAsync(string eventId, string orderId, CancellationToken ct)
    {
        var order = await LoadPendingAsync(eventId, "stock.reserved", orderId, ct);
        if (order is null)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        order.Confirm();
        db.Outbox.Add(OutboxEvents.Create("order.confirmed", order.Id,
            new OrderConfirmedData(order.Id, order.BuyerId, order.TotalCents)));

        var method = order.PaymentMethod.ToString().ToLowerInvariant();
        var paymentData = new PaymentData(order.Id, order.BuyerId, order.TotalCents, method);
        if (payment.Approve(order.Id))
        {
            order.MarkPaid();
            db.Outbox.Add(OutboxEvents.Create("payment.approved", order.Id, paymentData));
            db.Outbox.Add(OutboxEvents.Create("order.paid", order.Id, new OrderPaidData(
                order.Id,
                order.BuyerId,
                order.SellerIds,
                order.Items.Select(i => new OrderPaidItem(i.ProductId, i.SellerId, i.Quantity, i.PriceCents)).ToList(),
                order.TotalCents)));
        }
        else
        {
            order.Cancel();
            db.Outbox.Add(OutboxEvents.Create("payment.rejected", order.Id, paymentData));
            db.Outbox.Add(OutboxEvents.OrderCancelled(order, CancelReason.PaymentRejected));
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Pedido {OrderId} saiu de pending para {Status}", order.Id, order.Status);
    }

    public async Task HandleStockRejectedAsync(string eventId, string orderId, CancellationToken ct)
    {
        var order = await LoadPendingAsync(eventId, "stock.rejected", orderId, ct);
        if (order is not null)
        {
            order.Cancel();
            db.Outbox.Add(OutboxEvents.OrderCancelled(order, CancelReason.InsufficientStock));
            logger.LogInformation("Pedido {OrderId} cancelado por falta de estoque", order.Id);
        }

        await db.SaveChangesAsync(ct);
    }

    // Devolve o pedido pronto para transicionar, ou null quando não há o que fazer
    // (evento repetido, pedido desconhecido ou já fora de pending). Em todos os
    // casos o evento fica registrado como processado.
    private async Task<Order?> LoadPendingAsync(string eventId, string type, string orderId, CancellationToken ct)
    {
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct))
        {
            logger.LogInformation("Evento {EventId} já processado, ignorando", eventId);
            return null;
        }

        db.ProcessedEvents.Add(new ProcessedEvent(eventId, type));

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            logger.LogWarning("Evento {Type} para pedido desconhecido {OrderId}", type, orderId);
            return null;
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogWarning("Evento {Type} para pedido {OrderId} em {Status}, ignorando", type, orderId, order.Status);
            return null;
        }

        return order;
    }
}
