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

            entity.Property(e => e.Question).IsRequired();
            entity.Property(e => e.Answer).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IpHash).HasMaxLength(64);

            entity.HasIndex(e => e.Created);
            entity.HasIndex(e => e.SessionId);
        });
    }
}
