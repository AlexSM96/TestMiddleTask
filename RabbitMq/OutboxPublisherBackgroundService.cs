using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestMiddleTask.Contracts;
using TestMiddleTask.Database;
using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Outbox;

namespace TestMiddleTask.RabbitMq;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ITaskPublisher publisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));
        using var timer = new PeriodicTimer(pollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while processing outbox messages");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskAppDbContext>();
        var batchSize = Math.Max(1, _options.BatchSize);

        while (!ct.IsCancellationRequested)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var messages = await dbContext.OutboxMessages
                .FromSql($"""
                    SELECT * FROM "OutboxMessages"
                    WHERE "ProcessedAt" IS NULL AND "NextAttemptAt" <= {now}
                    ORDER BY "OccurredAt"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct);

            if (messages.Count == 0)
            {
                await transaction.CommitAsync(ct);
                return;
            }

            foreach (var message in messages)
            {
                await PublishMessageAsync(dbContext, message, ct);
            }

            await transaction.CommitAsync(ct);
        }
    }

    private async Task PublishMessageAsync(
        TaskAppDbContext dbContext,
        OutboxMessage message,
        CancellationToken ct)
    {
        try
        {
            if (message.Type != nameof(TaskCompletedMessage))
            {
                throw new NotSupportedException($"Unknown outbox message type '{message.Type}'.");
            }

            var taskCompletedMessage = JsonSerializer.Deserialize<TaskCompletedMessage>(message.Payload)
                ?? throw new JsonException($"Outbox message '{message.Id}' has an empty payload.");

            await publisher.PublishAsync(taskCompletedMessage, ct);

            message.ProcessedAt = DateTimeOffset.UtcNow;
            message.LastError = null;

            logger.LogInformation(
                "Outbox message {MessageId} for task {TaskId} published to RabbitMQ",
                message.Id,
                taskCompletedMessage.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            message.RetryCount++;
            message.LastError = Truncate(exception.Message, 2000);
            message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(GetRetryDelaySeconds(message.RetryCount));

            logger.LogError(
                exception,
                "Failed to publish outbox message {MessageId}, attempt {Attempt}",
                message.Id,
                message.RetryCount);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private int GetRetryDelaySeconds(int retryCount)
    {
        var delay = Math.Min(Math.Pow(2, retryCount), Math.Max(1, _options.MaxRetryDelaySeconds));
        return (int)delay;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
