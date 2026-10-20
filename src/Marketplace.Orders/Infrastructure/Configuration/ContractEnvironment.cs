namespace Marketplace.Orders.Infrastructure.Configuration;

// O CONTRATOS define variáveis planas (MYSQL_HOST) e o binder do .NET espera
// Secao:Chave. Este mapa traduz uma coisa na outra antes de ler a configuração.
public static class ContractEnvironment
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["PORT"] = "Port",
        ["MYSQL_HOST"] = "Database:Host",
        ["MYSQL_PORT"] = "Database:Port",
        ["MYSQL_USER"] = "Database:User",
        ["MYSQL_PASSWORD"] = "Database:Password",
        ["MYSQL_DATABASE"] = "Database:Name",
        ["KAFKA_BROKERS"] = "Kafka:Brokers",
        ["AUTH_URL"] = "Auth:Url",
        ["CATALOG_URL"] = "Catalog:Url",
        ["PAYMENT_APPROVAL_RATE"] = "Payment:ApprovalRate",
        ["LOG_LEVEL"] = "Logging:LogLevel:Default",
    };

    public static IConfigurationBuilder AddContractEnvironmentVariables(this IConfigurationBuilder builder)
    {
        var values = new Dictionary<string, string?>();
        foreach (var (variable, key) in Map)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) continue;

            values[key] = variable == "LOG_LEVEL" ? ToLogLevel(value) : value;
        }

        return builder.AddInMemoryCollection(values);
    }

    private static string ToLogLevel(string value) => value.ToLowerInvariant() switch
    {
        "trace" => "Trace",
        "debug" => "Debug",
        "warn" or "warning" => "Warning",
        "error" => "Error",
        _ => "Information",
    };
}
