namespace Marketplace.Orders.Infrastructure.Kafka;

public class KafkaOptions
{
    public const string Section = "Kafka";

    public string Brokers { get; set; } = "localhost:19092";

    public string GroupId { get; set; } = "marketplace-orders";
}
