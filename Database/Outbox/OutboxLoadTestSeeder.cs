using System.Text.Json;
using TestMiddleTask.Contracts;
using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Entities.Enums;

namespace TestMiddleTask.Database.Outbox;

/// <summary>
/// Creates completed tasks together with outbox messages. Used only for outbox/RabbitMQ load checks.
/// </summary>
public sealed class OutboxLoadTestSeeder(TaskAppDbContext context)
{
    public async Task<int> SeedAsync(int count, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tasks = new List<TaskItem>(count);
        var outboxMessages = new List<OutboxMessage>(count);

        for (var i = 0; i < count; i++)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = $"Outbox load test #{i + 1}",
                IsCompleted = true,
                CreatedAt = now,
                CompletedAt = now,
                Priority = (Priority)(i % 3),
            };

            tasks.Add(task);
            outboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(TaskCompletedMessage),
                Payload = JsonSerializer.Serialize(new TaskCompletedMessage(
                    task.Id,
                    task.Title,
                    task.Priority,
                    task.CreatedAt,
                    now)),
                OccurredAt = now,
                NextAttemptAt = now,
            });
        }

        await context.TaskItems.AddRangeAsync(tasks, cancellationToken);
        await context.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return tasks.Count;
    }
}
