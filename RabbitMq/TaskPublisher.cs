using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.RabbitMq;

public class TaskPublisher : ITaskPublisher
{
    public Task PublishAsync(TaskItem taskItem, CancellationToken ct)
    {
        // TODO: реализовать publisher
        throw new NotImplementedException();
    }
}