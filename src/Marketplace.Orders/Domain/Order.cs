namespace Marketplace.Orders.Domain;

public class Order
{
    public string Id { get; private set; } = null!;

    public string BuyerId { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public int TotalCents { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public List<OrderItem> Items { get; private set; } = [];

    public IReadOnlyList<string> SellerIds => Items.Select(i => i.SellerId).Distinct().ToList();

    private Order()
    {
    }

    public static Order Create(string buyerId, PaymentMethod paymentMethod, IEnumerable<OrderItem> items)
    {
        var now = DateTime.UtcNow;
        var order = new Order
        {
            // O id nasce antes do insert para entrar no evento do outbox na mesma transação.
            Id = Ulid.New("ord_"),
            BuyerId = buyerId,
            Status = OrderStatus.Pending,
            PaymentMethod = paymentMethod,
            Items = items.ToList(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (order.Items.Count == 0) throw new DomainException("order.empty", 422);

        order.TotalCents = order.Items.Sum(i => i.SubtotalCents);

        return order;
    }

    public bool IsOwnedBy(string userId) => BuyerId == userId;

    public bool CanBeCancelledByBuyer => Status is OrderStatus.Pending or OrderStatus.Confirmed;

    public void Confirm()
    {
        Transition(OrderStatus.Pending, OrderStatus.Confirmed);
    }

    public void MarkPaid()
    {
        Transition(OrderStatus.Confirmed, OrderStatus.Paid);
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Paid) throw new DomainException("order.cannot_cancel", 409);
        if (Status == OrderStatus.Cancelled) throw new DomainException("order.already_cancelled", 409);

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Transition(OrderStatus from, OrderStatus to)
    {
        if (Status != from) throw new DomainException("order.invalid_transition", 409);

        Status = to;
        UpdatedAt = DateTime.UtcNow;
    }
}
