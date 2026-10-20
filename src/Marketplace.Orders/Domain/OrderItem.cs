namespace Marketplace.Orders.Domain;

public class OrderItem
{
    public long Id { get; private set; }

    public string OrderId { get; private set; } = null!;

    public string ProductId { get; private set; } = null!;

    public string SellerId { get; private set; } = null!;

    public int Quantity { get; private set; }

    public int PriceCents { get; private set; }

    public int SubtotalCents => Quantity * PriceCents;

    private OrderItem()
    {
    }

    public OrderItem(string productId, string sellerId, int quantity, int priceCents)
    {
        ProductId = productId;
        SellerId = sellerId;
        Quantity = quantity;
        PriceCents = priceCents;
    }
}
