using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Contracts;

public record OrderResponse(
    string Id,
    string BuyerId,
    OrderStatus Status,
    int TotalCents,
    PaymentMethod PaymentMethod,
    IReadOnlyList<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.BuyerId,
        order.Status,
        order.TotalCents,
        order.PaymentMethod,
        order.Items.Select(OrderItemResponse.From).ToList(),
        order.CreatedAt,
        order.UpdatedAt);
}

public record OrderItemResponse(string ProductId, string SellerId, int Quantity, int PriceCents)
{
    public static OrderItemResponse From(OrderItem item) =>
        new(item.ProductId, item.SellerId, item.Quantity, item.PriceCents);
}
