namespace Marketplace.Orders.Infrastructure.Database;

public class DatabaseOptions
{
    public const string Section = "Database";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 3306;

    public string User { get; set; } = "root";

    public string Password { get; set; } = "root";

    public string Name { get; set; } = "marketplace_orders";

    public string ConnectionString =>
        $"Server={Host};Port={Port};User={User};Password={Password};Database={Name};CharSet=utf8mb4";
}
