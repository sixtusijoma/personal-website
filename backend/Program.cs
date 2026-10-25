using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

// Chat logging to Supabase Postgres. Optional: if SUPABASE_DB_CONNECTION is not set,
// no DbContext is registered and the chat endpoint simply skips logging.
var supabaseConnection = builder.Configuration["SUPABASE_DB_CONNECTION"];
if (!string.IsNullOrWhiteSpace(supabaseConnection))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(supabaseConnection).UseSnakeCaseNamingConvention());
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var geminiApiKey = builder.Configuration["GEMINI_API_KEY"];
var profile = builder.Configuration["PROFILE_JSON"];
var ipHashSalt = IpHasher.ResolveSalt(builder.Configuration, app.Environment, app.Logger);
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/api/chat", async (ChatRequest request, HttpContext httpContext, ILogger<Program> logger)  =>
{
    const string model = "gemini-3.5-flash-lite";
    var stopwatch = Stopwatch.StartNew();

    static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    async Task LogChatAsync(string answer, ChatStatus status, int? geminiStatusCode, string? interactionId)
    {
        try
        {
            var db = httpContext.RequestServices.GetService<AppDbContext>();
            if (db is null)
            {
                return;
            }

            db.ChatLogs.Add(new ChatLog
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId ?? Guid.Empty,
                Question = Truncate(request.Question ?? "", 2000),
                Answer = Truncate(answer, 8000),
                Status = status,
                GeminiStatusCode = geminiStatusCode,
                InteractionId = interactionId is null ? null : Truncate(interactionId, 256),
                PreviousInteractionId = request.PreviousInteractionId is null ? null : Truncate(request.PreviousInteractionId, 256),
                Model = model,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                // TODO: behind Cloudflare this is the proxy's IP. Use ForwardedHeaders
                // (UseForwardedHeaders with ForwardedHeaders = "CF-Connecting-IP") to get the real client IP.
                IpHash = IpHasher.Hash(httpContext.Connection.RemoteIpAddress?.ToString(), ipHashSalt),
                UserAgent = Truncate(httpContext.Request.Headers.UserAgent.ToString(), 512),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write chat log.");
        }
    }

    if (string.IsNullOrWhiteSpace(profile))
    {
        const string missingAnswer = "Profile is not configured.";
        await LogChatAsync(missingAnswer, ChatStatus.ProfileMissing, null, null);
        return Results.Ok(new { answer = missingAnswer });
    }

    int? geminiStatusCode = null;
    try
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-goog-api-key", geminiApiKey);

        var systemInstruction = $"""
        You answer questions about Sixtus Ijoma for visitors to his personal site.
        Use only the profile below. If the profile does not say, find a graceful way to say that you dont have access to that information. Your personality is warm and eager to answer questions about Sixtus.
        The user is typing to you on a mock command line interface, so you cannot use any markdown or special styling. DO NOT answer any questions unrelated to Sixtus.
        Profile:
        {profile}
        """;

        object payload = string.IsNullOrEmpty(request.PreviousInteractionId)
            ? new { model, input = request.Question, system_instruction = systemInstruction }
            : new { model, input = request.Question, previous_interaction_id = request.PreviousInteractionId, system_instruction = systemInstruction };

        var response = await client.PostAsJsonAsync(
            "https://generativelanguage.googleapis.com/v1beta/interactions",
            payload);
        geminiStatusCode = (int)response.StatusCode;

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var errorAnswer = $"Gemini returned {(int)response.StatusCode}. Try again in a minute.";
            await LogChatAsync(errorAnswer, ChatStatus.GeminiError, geminiStatusCode, null);
            return Results.Ok(new { answer = errorAnswer });
        }

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
        const string failureAnswer = "Something went wrong. Try again in a minute.";
        await LogChatAsync(failureAnswer, ChatStatus.Exception, geminiStatusCode, null);
        return Results.Ok(new { answer = failureAnswer });
    }
});
app.UseCors();



app.Run();

record ChatRequest(string Question, string? PreviousInteractionId, Guid? SessionId);



// after app.UseHttpsRedirection()




record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

