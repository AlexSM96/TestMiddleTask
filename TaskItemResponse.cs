using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Entities.Enums;

namespace TestMiddleTask;

public sealed record TaskItemResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority)
{
    public static TaskItemResponse FromEntity(TaskItem taskItem)
    {
        return new TaskItemResponse(
            taskItem.Id,
            taskItem.Title,
            taskItem.IsCompleted,
            taskItem.CreatedAt,
            taskItem.CompletedAt,
            taskItem.Priority);
    }
}
