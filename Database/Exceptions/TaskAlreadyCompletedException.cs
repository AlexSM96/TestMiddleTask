namespace TestMiddleTask.Database.Exceptions;

public sealed class TaskAlreadyCompletedException(Guid taskId)
    : Exception($"Task '{taskId}' is already completed.");
