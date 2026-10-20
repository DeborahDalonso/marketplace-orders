namespace Marketplace.Orders.Domain;

public static class CancelReason
{
    public const string InsufficientStock = "insufficient_stock";
    public const string PaymentRejected = "payment_rejected";
    public const string BuyerRequest = "buyer_request";
}
