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
                // TODO: behind Cloudflare this is the proxy's IP. Use ForwardedHeaders
                // (UseForwardedHeaders with ForwardedHeaders = "CF-Connecting-IP") to get the real client IP.
                IpHash = IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString(), ipHashSalt),
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
