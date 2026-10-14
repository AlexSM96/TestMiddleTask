using System.ComponentModel.DataAnnotations;
using TestMiddleTask.Database.Entities.Enums;

namespace TestMiddleTask;

public class CreateTaskItemRequest
{
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [AllowedValues(
        Priority.Low,
        Priority.Medium,
        Priority.High)]
    public Priority Priority { get; set; } =  Priority.Medium;
}