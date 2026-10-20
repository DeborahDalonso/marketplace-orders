using System.Reflection;
using Confluent.Kafka;
using Marketplace.Orders.Contracts;
using Marketplace.Orders.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Orders.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController(OrdersDbContext db, IProducer<string, string> producer) : ControllerBase
{
    private static readonly string Version =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthResponse>>> Get(CancellationToken ct)
    {
        var mysql = await db.Database.CanConnectAsync(ct) ? "ok" : "down";
        var health = new HealthResponse("marketplace-orders", Version, mysql, CheckKafka());
        var status = health.IsHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(status, ApiResponse<HealthResponse>.Ok(health));
    }

    // O admin client dependente reaproveita a conexão do producer; não abre outra.
    private string CheckKafka()
    {
        try
        {
            using var admin = new DependentAdminClientBuilder(producer.Handle).Build();
            admin.GetMetadata(TimeSpan.FromSeconds(2));

            return "ok";
        }
        catch (KafkaException)
        {
            return "down";
        }
    }
}
