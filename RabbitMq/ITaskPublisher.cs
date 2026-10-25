using TestMiddleTask.Contracts;

namespace TestMiddleTask.RabbitMq;

public interface ITaskPublisher
{
    public Task PublishAsync(TaskCompletedMessage message, CancellationToken ct);
}
