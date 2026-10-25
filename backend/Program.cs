using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string[] requiredKeys = ["GEMINI_API_KEY", "PROFILE_JSON", "SUPABASE_DB_CONNECTION", "IP_HASH_SALT"];
var missing = requiredKeys.Where(k => string.IsNullOrWhiteSpace(builder.Configuration[k])).ToList();

if (missing.Count > 0)
{
    throw new InvalidOperationException($"Missing required settings: {string.Join(", ", missing)}");
}

var geminiApiKey = builder.Configuration["GEMINI_API_KEY"]!;
var profile = builder.Configuration["PROFILE_JSON"]!;
var supabaseConnection = builder.Configuration["SUPABASE_DB_CONNECTION"]!;
var ipHashSalt = builder.Configuration["IP_HASH_SALT"]!;

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(supabaseConnection).UseSnakeCaseNamingConvention());

builder.Services.AddHttpClient("gemini", c =>
{
    c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    c.DefaultRequestHeaders.Add("x-goog-api-key", geminiApiKey);
});

var app = builder.Build();

app.UseHttpsRedirection();

var geminiJsonOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

app.MapPost("/api/chat", async (ChatRequest request, HttpContext httpContext, AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest();
    }

    const string model = "gemini-3.5-flash-lite";
    const string failureAnswer = "Something went wrong. Try again in a minute.";
    var stopwatch = Stopwatch.StartNew();
    var previousInteractionId = string.IsNullOrWhiteSpace(request.PreviousInteractionId) ? null : request.PreviousInteractionId;

    async Task LogChatAsync(string answer, ChatStatus status, int? geminiStatusCode, string? interactionId)
    {
        try
        {
            db.ChatLogs.Add(new ChatLog
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId ?? Guid.Empty,
                Question = request.Question,
                Answer = answer,
                Status = status,
                GeminiStatusCode = geminiStatusCode,
                InteractionId = interactionId,
                PreviousInteractionId = previousInteractionId,
                Model = model,
                Latency = (int)stopwatch.ElapsedMilliseconds,
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

    int? geminiStatusCode = null;
    try
    {
        var client = httpClientFactory.CreateClient("gemini");

        var systemInstruction = $"""
        You answer questions about Sixtus Ijoma for visitors to his personal site.
        Use only the profile below. If the profile does not say, find a graceful way to say that you don't have access to that information. Your personality is warm and eager to answer questions about Sixtus.
        The user is typing to you on a mock command line interface, so you cannot use any markdown or special styling. DO NOT answer any questions unrelated to Sixtus.
        Profile:
        {profile}
        """;

        var payload = new
        {
            model,
            input = request.Question,
            previous_interaction_id = previousInteractionId,
            system_instruction = systemInstruction
        };

        var response = await client.PostAsJsonAsync("v1beta/interactions", payload, geminiJsonOptions);
        geminiStatusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini returned status code {GeminiStatusCode}.", geminiStatusCode);
            await LogChatAsync(failureAnswer, ChatStatus.GeminiError, geminiStatusCode, null);
            return Results.Ok(new { answer = failureAnswer });
        }

        var body = await response.Content.ReadAsStringAsync();
        using var parsed = JsonDocument.Parse(body);
        var json = parsed.RootElement;
        var interactionId = json.TryGetProperty("id", out var id) ? id.GetString() : null;

        var answer = json.GetProperty("steps").EnumerateArray()
            .Last(s => s.GetProperty("type").GetString() == "model_output").
            GetProperty("content")[0].GetProperty("text").GetString() ?? "";

        await LogChatAsync(answer, ChatStatus.Ok, geminiStatusCode, interactionId);
        return Results.Ok(new { answer, interactionId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat request failed.");
        await LogChatAsync(failureAnswer, ChatStatus.Exception, geminiStatusCode, null);
        return Results.Ok(new { answer = failureAnswer });
    }
});
app.UseCors();

app.Run();

record ChatRequest(string Question, string? PreviousInteractionId, Guid? SessionId);
