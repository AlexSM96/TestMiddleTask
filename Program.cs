using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMiddleTask;
using TestMiddleTask.Database;
using TestMiddleTask.Database.Entities;
using TestMiddleTask.Database.Repositories;
using TestMiddleTask.RabbitMq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services
    .AddDbContext<TaskAppDbContext>(x =>
    {
        var connectionString = builder.Configuration.GetConnectionString("TaskDb");
        x.UseNpgsql(connectionString);
    });


builder.Services.AddScoped<ITaskDbRepository, TaskRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/tasks", async (ITaskDbRepository repository, CancellationToken ct) =>
{
    var tasks = await repository.GetTasks(ct);
    return tasks;
});


app.MapPost("/tasks", async (
    [FromServices] ITaskDbRepository repository,
    [FromBody] CreateTaskItemRequest createTaskItemRequest,
    CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(createTaskItemRequest.Title) || createTaskItemRequest.Title.Length > 200)
        {
            return Results.BadRequest("Title is required, max length is 200 characters.");
        }

        var createdTaskId = await repository.AddTask(new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = createTaskItemRequest.Title,
            Priority = createTaskItemRequest.Priority,
            CreatedAt = DateTimeOffset.UtcNow,
        }, ct);

        return Results.Ok(new { Id = createdTaskId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPut("/tasks/{id:guid}/complete", async (
    [FromServices] ITaskDbRepository repository,
    [FromServices] ITaskPublisher taskPublisher,
    [FromServices] ILogger logger,
    [FromQuery] Guid id,
    CancellationToken ct) =>
{
    try
    {
        var completedTask = await repository.CompleteTask(id, ct);
        if (completedTask is null)
        {
            return Results.NotFound();
        }

        // TODO: плохо -> переделать на Outbox
        _ = Task.Run(() =>
        {
            try
            {
                taskPublisher.PublishAsync(completedTask, ct);
            }
            catch (Exception e)
            {
                logger.LogError($"Ошибка при отправке сообщения: {e.Message}");
            }
        }, ct);

        return Results.Ok(new { completedTask.Id });
    }
    catch (DbUpdateConcurrencyException ex)
    {
        return Results.Conflict("Кто-то уже завершает данную задачу");
    }
    catch (Exception e)
    {
        return Results.BadRequest(e.Message);
    }
});

app.MapDelete("/tasks/{id:guid}", async (
    [FromServices]ITaskDbRepository repository,
    [FromQuery] Guid id,
    CancellationToken ct) =>
{
    try
    {
        var count = await repository.DeleteTask(id, ct);
        return Results.Ok(new { CountDeletedRow = count });
    }
    catch (Exception e)
    {
        return Results.BadRequest(e.Message);
    }
});

app.Run();