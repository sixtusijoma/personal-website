using Microsoft.EntityFrameworkCore;

namespace backend.Data;

// Deletes chat_logs rows older than 90 days, once at startup and then every 12 hours.
public class ChatLogRetentionService(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatLogRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(90);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await PurgeAsync(stoppingToken);
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task PurgeAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cutoff = DateTimeOffset.UtcNow - MaxAge;
            var deleted = await db.ChatLogs
                .Where(l => l.CreatedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);
            logger.LogInformation("Chat log retention: deleted {Count} rows older than {Cutoff:o}.", deleted, cutoff);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat log retention purge failed.");
        }
    }
}
