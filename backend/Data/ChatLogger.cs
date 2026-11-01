using System.Diagnostics;

namespace backend.Data;

public record ChatLogContext(ChatRequest Request, string? PreviousInteractionId, string Model, Stopwatch Stopwatch);

public class ChatLogger(AppDbContext db, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<ChatLogger> logger)
{
    private readonly string ipHashSalt = configuration["IP_HASH_SALT"]!;

    public async Task Log(ChatLogContext context, string answer, ChatStatus status, int? geminiStatusCode, string? interactionId)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext!;

            // Kestrel listens dual-stack, so an IPv4 peer can arrive as ::ffff:a.b.c.d; hash one form per address.
            var remoteIp = httpContext.Connection.RemoteIpAddress;
            if (remoteIp is { IsIPv4MappedToIPv6: true })
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            db.ChatLogs.Add(new ChatLog
            {
                Id = Guid.NewGuid(),
                SessionId = context.Request.SessionId ?? Guid.Empty,
                Question = context.Request.Question,
                Answer = answer,
                Status = status,
                GeminiStatusCode = geminiStatusCode,
                InteractionId = interactionId,
                PreviousInteractionId = context.PreviousInteractionId,
                Model = context.Model,
                Latency = (int)context.Stopwatch.ElapsedMilliseconds,
                IpHash = IpHasher.Hash(remoteIp?.ToString(), ipHashSalt),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                Created = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write chat log.");
        }
    }
}
