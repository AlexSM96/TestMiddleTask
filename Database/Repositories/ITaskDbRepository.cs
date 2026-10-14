using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.Database.Repositories;

public interface ITaskDbRepository
{
    public Task<ICollection<TaskItem>> GetTasks(CancellationToken cancellationToken);

    public Task<Guid> AddTask(TaskItem task, CancellationToken cancellationToken);

    public Task<TaskItem?> CompleteTask(Guid taskId, CancellationToken cancellationToken);

    public Task<int> DeleteTask(Guid taskId, CancellationToken cancellationToken);
}