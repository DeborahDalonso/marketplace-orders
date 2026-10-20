using Confluent.Kafka;
using Marketplace.Orders.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Orders.Infrastructure.Kafka;

// Lado produtor do Outbox: a cada ciclo publica o que ainda não tem published_at.
public class OutboxPublisher(
    IServiceScopeFactory scopes,
    IProducer<string, string> producer,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PublishPendingAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Parada normal do host.
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var pending = await db.Outbox
                .Where(m => m.PublishedAt == null)
                .OrderBy(m => m.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            foreach (var message in pending)
            {
                var kafkaMessage = new Message<string, string> { Key = message.AggregateId, Value = message.Payload };
                await producer.ProduceAsync(message.Type, kafkaMessage, ct);
                message.MarkPublished();

                // Marca um a um: se cair no meio do lote, no máximo republica
                // uma mensagem, e todo consumidor do ecossistema é idempotente.
                await db.SaveChangesAsync(ct);
            }

            if (pending.Count > 0) logger.LogInformation("{Count} evento(s) publicados no Kafka", pending.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao publicar o outbox; tenta de novo no próximo ciclo");
        }
    }
}
