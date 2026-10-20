using Marketplace.Orders.Contracts;
using Marketplace.Orders.Contracts.Events;
using Marketplace.Orders.Domain;
using Marketplace.Orders.Infrastructure.Catalog;
using Marketplace.Orders.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Orders.Application;

public class OrderService(OrdersDbContext db, CatalogClient catalog, ILogger<OrderService> logger)
{
    private const int MaxPerPage = 100;

    /// <summary>Cria o pedido como pending e grava order.created no outbox na mesma transação.</summary>
    public async Task<Order> CreateAsync(string buyerId, CreateOrderRequest request, CancellationToken ct)
    {
        var items = new List<OrderItem>();
        foreach (var requested in request.Items)
        {
            var product = await catalog.GetProductAsync(requested.ProductId, ct)
                ?? throw new DomainException("order.product_not_found", 422);
            if (!product.Active) throw new DomainException("order.product_inactive", 422);

            items.Add(new OrderItem(product.Id, product.SellerId, requested.Quantity, product.PriceCents));
        }

        var paymentMethod = Enum.Parse<PaymentMethod>(request.PaymentMethod, true);
        var order = Order.Create(buyerId, paymentMethod, items);

        db.Orders.Add(order);
        db.Outbox.Add(OutboxEvents.Create("order.created", order.Id, new OrderCreatedData(
            order.Id,
            order.BuyerId,
            order.Items.Select(i => new OrderCreatedItem(i.ProductId, i.Quantity)).ToList(),
            order.TotalCents)));

        // Um único SaveChanges: o EF já envolve pedido, itens e outbox na mesma transação.
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Pedido {OrderId} criado para {BuyerId}, total {TotalCents}", order.Id, buyerId, order.TotalCents);

        return order;
    }

    /// <summary>Busca um pedido. Só o dono ou um admin podem ver.</summary>
    public async Task<Order> GetAsync(string orderId, string userId, bool isAdmin, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("order.not_found", 404);
        if (!isAdmin && !order.IsOwnedBy(userId)) throw new DomainException("order.forbidden", 403);

        return order;
    }

    public async Task<PagedResult<OrderResponse>> ListMineAsync(string buyerId, int page, int perPage, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.BuyerId == buyerId);

        return await PageAsync(query, page, perPage, ct);
    }

    /// <summary>Lista para admin (tudo) ou vendedor (só os pedidos com item dele).</summary>
    public async Task<PagedResult<OrderResponse>> ListAsync(
        string? status, string? sellerId, string userId, bool isAdmin, int page, int perPage, CancellationToken ct)
    {
        // REGRA: vendedor só enxerga a própria lista; o filtro sellerId é obrigatório e tem que ser ele.
        if (!isAdmin && sellerId != userId) throw new DomainException("order.forbidden", 403);

        var query = db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsed))
            {
                throw new DomainException("order.invalid_status", 422);
            }

            query = query.Where(o => o.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(sellerId))
        {
            query = query.Where(o => o.Items.Any(i => i.SellerId == sellerId));
        }

        return await PageAsync(query, page, perPage, ct);
    }

    /// <summary>Cancela o pedido a pedido do comprador. Só vale em pending ou confirmed.</summary>
    public async Task<Order> CancelAsync(string orderId, string buyerId, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("order.not_found", 404);
        if (!order.IsOwnedBy(buyerId)) throw new DomainException("order.forbidden", 403);
        if (!order.CanBeCancelledByBuyer) throw new DomainException("order.cannot_cancel", 409);

        order.Cancel();
        db.Outbox.Add(OutboxEvents.OrderCancelled(order, CancelReason.BuyerRequest));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Pedido {OrderId} cancelado pelo comprador", order.Id);

        return order;
    }

    private static async Task<PagedResult<OrderResponse>> PageAsync(
        IQueryable<Order> query, int page, int perPage, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        perPage = Math.Clamp(perPage, 1, MaxPerPage);

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Include(o => o.Items)
            .ToListAsync(ct);

        return new PagedResult<OrderResponse>(orders.Select(OrderResponse.From).ToList(), page, perPage, total);
    }
}
