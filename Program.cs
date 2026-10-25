using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestMiddleTask;
using TestMiddleTask.Database;
using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Exceptions;
using TestMiddleTask.Database.Outbox;
using TestMiddleTask.Database.Repositories;
using TestMiddleTask.RabbitMq;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TaskDb")
    ?? throw new InvalidOperationException("Connection string 'TaskDb' is not configured.");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services
    .AddDbContext<TaskAppDbContext>(x =>
    {
        x.UseNpgsql(connectionString);
    });

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection(OutboxOptions.SectionName));

builder.Services.AddScoped<ITaskDbRepository, TaskRepository>();
builder.Services.AddSingleton<ITaskPublisher, TaskPublisher>();
builder.Services.AddHostedService<OutboxPublisherBackgroundService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskAppDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badHttpRequestException
        ? badHttpRequestException.StatusCode
        : StatusCodes.Status500InternalServerError,
});

app.MapGet("/tasks", async (ITaskDbRepository repository, CancellationToken ct) =>
{
    var tasks = await repository.GetTasks(ct);
    return Results.Ok(tasks.Select(TaskItemResponse.FromEntity));
});

app.MapPost("/tasks", async (
    CreateTaskItemRequest createTaskItemRequest,
    ITaskDbRepository repository,
    CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(createTaskItemRequest.Title))
    {
        errors[nameof(createTaskItemRequest.Title)] = new[] { "Title is required." };
    }
    else if (createTaskItemRequest.Title.Length > 200)
    {
        errors[nameof(createTaskItemRequest.Title)] = new[] { "Title max length is 200 characters." };
    }

    if (!Enum.IsDefined(createTaskItemRequest.Priority))
    {
        errors[nameof(createTaskItemRequest.Priority)] = new[] { "Unknown priority value." };
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var createdTaskId = await repository.AddTask(new TaskItem
    {
        Id = Guid.NewGuid(),
        Title = createTaskItemRequest.Title,
        Priority = createTaskItemRequest.Priority,
        CreatedAt = DateTimeOffset.UtcNow,
    }, ct);

    return Results.Created($"/tasks/{createdTaskId}", new { Id = createdTaskId });
});

app.MapPut("/tasks/{id:guid}/complete", async (
    Guid id,
    ITaskDbRepository repository,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        var completedTask = await repository.CompleteTask(id, ct);
        if (completedTask is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { completedTask.Id });
    }
    catch (TaskAlreadyCompletedException)
    {
        return Results.Conflict("Задача уже завершена.");
    }
    catch (DbUpdateConcurrencyException exception)
    {
        logger.LogWarning(exception, "Concurrency conflict while completing task {TaskId}", id);
        return Results.Conflict("Кто-то уже завершает данную задачу");
    }
});

app.MapDelete("/tasks/{id:guid}", async (
    Guid id,
    ITaskDbRepository repository,
    CancellationToken ct) =>
{
    var count = await repository.DeleteTask(id, ct);
    return count == 0
        ? Results.NotFound()
        : Results.Ok(new { CountDeletedRow = count });
});

app.Run();
