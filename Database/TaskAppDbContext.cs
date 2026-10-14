using Microsoft.EntityFrameworkCore;
using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.Database;

public sealed class TaskAppDbContext(DbContextOptions<TaskAppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Вынести конфигурацию в отдельные файлы
        modelBuilder
            .Entity<TaskItem>()
            .Property(x => x.Id)
            .ValueGeneratedOnAdd();
        
        modelBuilder
            .Entity<TaskItem>()
            .Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        modelBuilder
            .Entity<TaskItem>()
            .Property(x => x.RowVersion)
            .IsRowVersion();
    }
}