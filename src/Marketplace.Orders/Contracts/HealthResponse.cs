namespace Marketplace.Orders.Contracts;

public record HealthResponse(string Service, string Version, string Mysql, string Kafka)
{
    public bool IsHealthy => Mysql == "ok" && Kafka == "ok";
}
