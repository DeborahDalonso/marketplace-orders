namespace Marketplace.Orders.Contracts.Events;

public record OrderCreatedData(string OrderId, string BuyerId, IReadOnlyList<OrderCreatedItem> Items, int TotalCents);

public record OrderCreatedItem(string ProductId, int Quantity);

public record OrderConfirmedData(string OrderId, string BuyerId, int TotalCents);

public record OrderCancelledData(string OrderId, string BuyerId, string Reason);

public record PaymentData(string OrderId, string BuyerId, int TotalCents, string Method);

public record OrderPaidData(
    string OrderId,
    string BuyerId,
    IReadOnlyList<string> SellerIds,
    IReadOnlyList<OrderPaidItem> Items,
    int TotalCents);

public record OrderPaidItem(string ProductId, string SellerId, int Quantity, int PriceCents);
