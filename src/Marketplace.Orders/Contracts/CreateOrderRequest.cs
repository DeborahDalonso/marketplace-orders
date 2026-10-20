namespace Marketplace.Orders.Contracts;

public record CreateOrderRequest(List<OrderItemRequest> Items, string PaymentMethod);

public record OrderItemRequest(string ProductId, int Quantity);
