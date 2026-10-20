using System.Text.Json;
using Marketplace.Orders.Application;
using Marketplace.Orders.Contracts;
using Marketplace.Orders.Contracts.Events;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Orders.Tests;

public class OrderServiceTests : IDisposable
{
    private const string Buyer = "usr_01BUYER000000000000000000";
    private const string Other = "usr_01OTHER000000000000000000";

    private readonly TestDb _db = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_db.Context, FakeCatalogHandler.WithDefaults().AsClient(), NullLogger<OrderService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static CreateOrderRequest Request(params (string ProductId, int Quantity)[] items) =>
        new(items.Select(i => new OrderItemRequest(i.ProductId, i.Quantity)).ToList(), "pix");

    [Fact]
    public async Task Cria_pedido_pendente_com_total_correto()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Headphone.Id, 2), (FakeCatalogHandler.Cable.Id, 3)), default);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2 * 19990 + 3 * 2990, order.TotalCents);
        Assert.Equal(PaymentMethod.Pix, order.PaymentMethod);

        var saved = await _db.CreateContext().Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal(order.Id, saved.Id);
        Assert.Equal(2, saved.Items.Count);
        Assert.Equal(FakeCatalogHandler.Headphone.SellerId, saved.Items[0].SellerId);
    }

    [Fact]
    public async Task Grava_order_created_no_outbox_junto_com_o_pedido()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Headphone.Id, 1)), default);

        var message = await _db.CreateContext().Outbox.SingleAsync();
        Assert.Equal("order.created", message.Type);
        Assert.Equal(order.Id, message.AggregateId);
        Assert.Null(message.PublishedAt);

        var envelope = JsonSerializer.Deserialize<EventEnvelope<OrderCreatedData>>(message.Payload, EventEnvelope<OrderCreatedData>.JsonOptions)!;
        Assert.StartsWith("evt_", envelope.EventId);
        Assert.Equal("marketplace-orders", envelope.Producer);
        Assert.Equal(1, envelope.Version);
        Assert.Equal(order.Id, envelope.Data.OrderId);
        Assert.Equal(19990, envelope.Data.TotalCents);
        Assert.Single(envelope.Data.Items);
    }

    [Fact]
    public async Task Recusa_produto_que_nao_existe_no_catalogo()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(Buyer, Request(("prd_01NAOEXISTE0000000000000", 1)), default));

        Assert.Equal("order.product_not_found", ex.Code);
        Assert.Equal(422, ex.Status);
        Assert.Empty(await _db.CreateContext().Orders.ToListAsync());
    }

    [Fact]
    public async Task Recusa_produto_inativo()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Inactive.Id, 1)), default));

        Assert.Equal("order.product_inactive", ex.Code);
    }

    [Fact]
    public async Task Dono_e_admin_veem_o_pedido_e_outro_comprador_nao()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);

        Assert.Equal(order.Id, (await _service.GetAsync(order.Id, Buyer, false, default)).Id);
        Assert.Equal(order.Id, (await _service.GetAsync(order.Id, Other, true, default)).Id);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.GetAsync(order.Id, Other, false, default));
        Assert.Equal(403, ex.Status);
    }

    [Fact]
    public async Task Devolve_404_para_pedido_inexistente()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.GetAsync("ord_01NAOEXISTE0000000000000", Buyer, true, default));

        Assert.Equal("order.not_found", ex.Code);
        Assert.Equal(404, ex.Status);
    }

    [Fact]
    public async Task Lista_so_os_pedidos_do_comprador_paginados()
    {
        for (var i = 0; i < 3; i++) await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);
        await _service.CreateAsync(Other, Request((FakeCatalogHandler.Cable.Id, 1)), default);

        var page = await _service.ListMineAsync(Buyer, 1, 2, default);

        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, o => Assert.Equal(Buyer, o.BuyerId));
    }

    [Fact]
    public async Task Admin_filtra_por_status_e_vendedor()
    {
        var cancelled = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);
        await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Headphone.Id, 1)), default);
        await _service.CancelAsync(cancelled.Id, Buyer, default);

        var byStatus = await _service.ListAsync("cancelled", null, "usr_admin", true, 1, 20, default);
        var bySeller = await _service.ListAsync(null, FakeCatalogHandler.Headphone.SellerId, "usr_admin", true, 1, 20, default);

        Assert.Equal([cancelled.Id], byStatus.Items.Select(o => o.Id));
        Assert.Single(bySeller.Items);
        Assert.Equal(FakeCatalogHandler.Headphone.Id, bySeller.Items[0].Items[0].ProductId);
    }

    [Fact]
    public async Task Vendedor_so_lista_os_proprios_pedidos()
    {
        var seller = FakeCatalogHandler.Cable.SellerId;

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.ListAsync(null, "usr_outro", seller, false, 1, 20, default));
        Assert.Equal(403, ex.Status);

        var own = await _service.ListAsync(null, seller, seller, false, 1, 20, default);
        Assert.Equal(0, own.Total);
    }

    [Fact]
    public async Task Recusa_filtro_de_status_desconhecido()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.ListAsync("shipped", null, "usr_admin", true, 1, 20, default));

        Assert.Equal("order.invalid_status", ex.Code);
    }

    [Fact]
    public async Task Cancela_pedido_pendente_e_grava_order_cancelled()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);

        var cancelled = await _service.CancelAsync(order.Id, Buyer, default);

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        var message = await _db.CreateContext().Outbox.SingleAsync(m => m.Type == "order.cancelled");
        var envelope = JsonSerializer.Deserialize<EventEnvelope<OrderCancelledData>>(message.Payload, EventEnvelope<OrderCancelledData>.JsonOptions)!;
        Assert.Equal(CancelReason.BuyerRequest, envelope.Data.Reason);
    }

    [Fact]
    public async Task Nao_deixa_outro_comprador_cancelar()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CancelAsync(order.Id, Other, default));

        Assert.Equal("order.forbidden", ex.Code);
    }

    [Fact]
    public async Task Nao_cancela_pedido_ja_cancelado()
    {
        var order = await _service.CreateAsync(Buyer, Request((FakeCatalogHandler.Cable.Id, 1)), default);
        await _service.CancelAsync(order.Id, Buyer, default);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CancelAsync(order.Id, Buyer, default));

        Assert.Equal("order.cannot_cancel", ex.Code);
        Assert.Equal(409, ex.Status);
    }
}
