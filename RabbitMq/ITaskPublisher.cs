using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.RabbitMq;

public interface ITaskPublisher
{
    public Task PublishAsync(TaskItem taskItem, CancellationToken ct);
}