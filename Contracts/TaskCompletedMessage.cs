using TestMiddleTask.Database.Entities.Enums;

namespace TestMiddleTask.Contracts;

public sealed record TaskCompletedMessage(
    Guid Id,
    string Title,
    Priority Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset CompletedAt);
