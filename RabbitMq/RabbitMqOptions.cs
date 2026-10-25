namespace TestMiddleTask.RabbitMq;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "task_user";

    public string Password { get; set; } = "task_password";

    public string QueueName { get; set; } = "TaskQueue";
}
