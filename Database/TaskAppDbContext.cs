using Microsoft.EntityFrameworkCore;
using TestMiddleTask.Database.Entities;

namespace TestMiddleTask.Database;

public sealed class TaskAppDbContext(DbContextOptions<TaskAppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    
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

        modelBuilder
            .Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Payload).IsRequired();
                entity.Property(x => x.LastError).HasMaxLength(2000);
                entity.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt });
            });
    }
}