using Marketplace.Orders.Infrastructure.Catalog;
using Marketplace.Orders.Infrastructure.Database;
using Marketplace.Orders.Infrastructure.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Orders.Tests.Support;

// Sobe a API inteira em memória: SQLite no lugar do MySQL, catalog falso,
// JWT assinado com a chave de teste e sem os workers do Kafka.
public class OrdersApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<OrdersDbContext>(options => options.UseSqlite(_connection));

            foreach (var worker in services.Where(IsKafkaWorker).ToList())
            {
                services.Remove(worker);
            }

            services.AddHttpClient<CatalogClient>()
                .ConfigurePrimaryHttpMessageHandler(() => FakeCatalogHandler.WithDefaults());

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.ConfigurationManager = null;
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.Key;
            });
        });
    }

    public HttpClient CreateClientAs(string userId, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.For(userId, role));

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    private static bool IsKafkaWorker(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IHostedService)
        && descriptor.ImplementationType is { } type
        && (type == typeof(OutboxPublisher) || type == typeof(StockEventsConsumer));
}
