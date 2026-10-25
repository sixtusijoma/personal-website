using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ChatLog> ChatLogs => Set<ChatLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatLog>(entity =>
        {
            entity.ToTable("chat_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Question).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Answer).HasMaxLength(8000).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(e => e.InteractionId).HasMaxLength(256);
            entity.Property(e => e.PreviousInteractionId).HasMaxLength(256);
            entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SessionId);
        });
    }
}
