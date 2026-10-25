using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestMiddleTask.Contracts;
using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Exceptions;

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

        if (taskToUpdate.IsCompleted)
        {
            throw new TaskAlreadyCompletedException(taskId);
        }

        var completedAt = DateTimeOffset.UtcNow;
        taskToUpdate.IsCompleted = true;
        taskToUpdate.CompletedAt = completedAt;

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(TaskCompletedMessage),
            Payload = JsonSerializer.Serialize(new TaskCompletedMessage(
                taskToUpdate.Id,
                taskToUpdate.Title,
                taskToUpdate.Priority,
                taskToUpdate.CreatedAt,
                completedAt)),
            OccurredAt = completedAt,
            NextAttemptAt = completedAt,
        });

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