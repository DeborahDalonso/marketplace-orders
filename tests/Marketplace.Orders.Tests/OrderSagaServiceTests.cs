using System.Text.Json;
using Marketplace.Orders.Application;
using Marketplace.Orders.Contracts.Events;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Orders.Tests;

public class OrderSagaServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private OrderSagaService SagaWithApprovalRate(double rate) => new(
        _db.Context,
        new PaymentSimulator(Options.Create(new PaymentOptions { ApprovalRate = rate }), NullLogger<PaymentSimulator>.Instance),
        NullLogger<OrderSagaService>.Instance);

    private async Task<Order> SeedPendingOrderAsync()
    {
        var order = Order.Create("usr_01BUYER000000000000000000", PaymentMethod.Card,
        [
            new OrderItem("prd_a", "usr_seller_a", 2, 19990),
            new OrderItem("prd_b", "usr_seller_b", 1, 2990),
        ]);
        _db.Context.Orders.Add(order);
        await _db.Context.SaveChangesAsync();

        return order;
    }

    private async Task<List<string>> OutboxTypesAsync() =>
        await _db.CreateContext().Outbox.OrderBy(m => m.Id).Select(m => m.Type).ToListAsync();

    [Fact]
    public async Task Estoque_reservado_com_pagamento_aprovado_leva_a_paid()
    {
        var order = await SeedPendingOrderAsync();

        await SagaWithApprovalRate(1).HandleStockReservedAsync("evt_1", order.Id, default);

        var saved = await _db.CreateContext().Orders.SingleAsync();
        Assert.Equal(OrderStatus.Paid, saved.Status);
        Assert.Equal(["order.confirmed", "payment.approved", "order.paid"], await OutboxTypesAsync());
    }

    [Fact]
    public async Task Order_paid_leva_vendedores_e_itens()
    {
        var order = await SeedPendingOrderAsync();

        await SagaWithApprovalRate(1).HandleStockReservedAsync("evt_1", order.Id, default);

        var message = await _db.CreateContext().Outbox.SingleAsync(m => m.Type == "order.paid");
        var envelope = JsonSerializer.Deserialize<EventEnvelope<OrderPaidData>>(message.Payload, EventEnvelope<OrderPaidData>.JsonOptions)!;
        Assert.Equal(["usr_seller_a", "usr_seller_b"], envelope.Data.SellerIds);
        Assert.Equal(2, envelope.Data.Items.Count);
        Assert.Equal(2 * 19990 + 2990, envelope.Data.TotalCents);

        var payment = await _db.CreateContext().Outbox.SingleAsync(m => m.Type == "payment.approved");
        Assert.Contains("\"method\":\"card\"", payment.Payload);
    }

    [Fact]
    public async Task Estoque_reservado_com_pagamento_rejeitado_cancela()
    {
        var order = await SeedPendingOrderAsync();

        await SagaWithApprovalRate(0).HandleStockReservedAsync("evt_1", order.Id, default);

        var saved = await _db.CreateContext().Orders.SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        Assert.Equal(["order.confirmed", "payment.rejected", "order.cancelled"], await OutboxTypesAsync());

        var cancelled = await _db.CreateContext().Outbox.SingleAsync(m => m.Type == "order.cancelled");
        Assert.Contains(CancelReason.PaymentRejected, cancelled.Payload);
    }

    [Fact]
    public async Task Estoque_rejeitado_cancela_por_falta_de_estoque()
    {
        var order = await SeedPendingOrderAsync();

        await SagaWithApprovalRate(1).HandleStockRejectedAsync("evt_1", order.Id, default);

        var saved = await _db.CreateContext().Orders.SingleAsync();
        Assert.Equal(OrderStatus.Cancelled, saved.Status);
        var message = await _db.CreateContext().Outbox.SingleAsync();
        Assert.Equal("order.cancelled", message.Type);
        Assert.Contains(CancelReason.InsufficientStock, message.Payload);
    }

    [Fact]
    public async Task Ignora_evento_repetido()
    {
        var order = await SeedPendingOrderAsync();
        var saga = SagaWithApprovalRate(1);

        await saga.HandleStockReservedAsync("evt_1", order.Id, default);
        await saga.HandleStockReservedAsync("evt_1", order.Id, default);

        Assert.Equal(3, (await OutboxTypesAsync()).Count);
        Assert.Single(await _db.CreateContext().ProcessedEvents.ToListAsync());
    }

    [Fact]
    public async Task Ignora_evento_para_pedido_que_ja_saiu_de_pending()
    {
        var order = await SeedPendingOrderAsync();

        await SagaWithApprovalRate(1).HandleStockReservedAsync("evt_1", order.Id, default);
        await SagaWithApprovalRate(1).HandleStockRejectedAsync("evt_2", order.Id, default);

        var saved = await _db.CreateContext().Orders.SingleAsync();
        Assert.Equal(OrderStatus.Paid, saved.Status);
        Assert.Equal(2, await _db.CreateContext().ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task Marca_como_processado_evento_de_pedido_desconhecido()
    {
        await SagaWithApprovalRate(1).HandleStockReservedAsync("evt_1", "ord_01NAOEXISTE0000000000000", default);

        Assert.Single(await _db.CreateContext().ProcessedEvents.ToListAsync());
        Assert.Empty(await OutboxTypesAsync());
    }
}
