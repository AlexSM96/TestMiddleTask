using Microsoft.EntityFrameworkCore;
using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.Database.Repositories;

public class TaskRepository(TaskAppDbContext context) : ITaskDbRepository
{
    
    public async Task<ICollection<TaskItem>> GetTasks(CancellationToken cancellationToken)
    {
        return await context
            .TaskItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddTask(TaskItem task, CancellationToken cancellationToken)
    {
        var created = await context
            .TaskItems
            .AddAsync(task, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return created.Entity.Id;
    }

    public async Task<TaskItem?> CompleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        var taskToUpdate = await context.TaskItems.FindAsync([taskId], cancellationToken);
        if (taskToUpdate is null)
        {
            return null;
        }

        taskToUpdate.IsCompleted = true;
        taskToUpdate.CompletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        return taskToUpdate;
    }

    public async Task<int> DeleteTask(Guid taskId, CancellationToken cancellationToken)
    {
        return await context.TaskItems
            .Where(x => x.Id == taskId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}