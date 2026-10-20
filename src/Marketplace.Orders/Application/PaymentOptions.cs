namespace Marketplace.Orders.Application;

public class PaymentOptions
{
    public const string Section = "Payment";

    public double ApprovalRate { get; set; } = 0.9;
}
