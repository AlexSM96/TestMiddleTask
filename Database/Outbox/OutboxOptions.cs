namespace TestMiddleTask.Database.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 20;

    public int PollingIntervalSeconds { get; set; } = 1;

    public int MaxRetryDelaySeconds { get; set; } = 60;
}
