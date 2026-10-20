using System.Text.Json;
using Confluent.Kafka;
using Marketplace.Orders.Application;
using Marketplace.Orders.Contracts.Events;
using Microsoft.Extensions.Options;

namespace Marketplace.Orders.Infrastructure.Kafka;

// Consome stock.reserved e stock.rejected do catalog e entrega ao OrderSagaService.
// Commit manual depois de processar; se o processamento falhar, volta o offset
// e tenta de novo, para não perder a transição de um pedido.
public class StockEventsConsumer(
    IServiceScopeFactory scopes,
    IOptions<KafkaOptions> options,
    ILogger<StockEventsConsumer> logger) : BackgroundService
{
    private static readonly string[] Topics = ["stock.reserved", "stock.rejected"];
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    // Consume é bloqueante; roda numa thread própria para não segurar o host.
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Factory.StartNew(() => Run(stoppingToken), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private void Run(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.Brokers,
            GroupId = options.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.LogWarning("Kafka: {Reason}", error.Reason))
            .Build();
        consumer.Subscribe(Topics);
        logger.LogInformation("Consumindo {Topics} como grupo {GroupId}", string.Join(", ", Topics), config.GroupId);

        while (!ct.IsCancellationRequested)
        {
            ConsumeResult<string, string> result;
            try
            {
                result = consumer.Consume(ct);
            }
            catch (ConsumeException ex)
            {
                logger.LogError("Falha ao consumir: {Reason}", ex.Error.Reason);
                continue;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                Handle(result, ct);
                consumer.Commit(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao processar {Topic}@{Offset}; tenta de novo", result.Topic, result.Offset);
                consumer.Seek(result.TopicPartitionOffset);
                ct.WaitHandle.WaitOne(RetryDelay);
            }
        }

        consumer.Close();
    }

    private void Handle(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var envelope = Parse(result.Message.Value);
        if (envelope is null)
        {
            logger.LogWarning("Mensagem inválida em {Topic}@{Offset}, descartada", result.Topic, result.Offset);
            return;
        }

        using var scope = scopes.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderSagaService>();
        var task = result.Topic switch
        {
            "stock.reserved" => saga.HandleStockReservedAsync(envelope.EventId, envelope.Data.OrderId, ct),
            "stock.rejected" => saga.HandleStockRejectedAsync(envelope.EventId, envelope.Data.OrderId, ct),
            _ => Task.CompletedTask,
        };

        task.GetAwaiter().GetResult();
    }

    private static EventEnvelope<StockEventData>? Parse(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<StockEventData>>(json, EventEnvelope<StockEventData>.JsonOptions);
            var complete = !string.IsNullOrEmpty(envelope?.EventId) && !string.IsNullOrEmpty(envelope.Data?.OrderId);

            return complete ? envelope : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
