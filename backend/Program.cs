using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using backend.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using IPNetwork = System.Net.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

string[] requiredKeys = ["GEMINI_API_KEY", "PROFILE_JSON", "SUPABASE_DB_CONNECTION", "IP_HASH_SALT", "BIRTH_DATE", "CORS_ALLOWED_ORIGINS"];
var missing = requiredKeys.Where(k => string.IsNullOrWhiteSpace(builder.Configuration[k])).ToList();

if (missing.Count > 0)
{
    throw new InvalidOperationException($"Missing required settings: {string.Join(", ", missing)}");
}

var geminiApiKey = builder.Configuration["GEMINI_API_KEY"]!;
var profile = builder.Configuration["PROFILE_JSON"]!;
var supabaseConnection = builder.Configuration["SUPABASE_DB_CONNECTION"]!;

if (!DateOnly.TryParseExact(builder.Configuration["BIRTH_DATE"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
{
    throw new InvalidOperationException("BIRTH_DATE must be in yyyy-MM-dd format.");
}

var allowedOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"]!
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST")
            .WithHeaders("Content-Type")
            .SetPreflightMaxAge(TimeSpan.FromHours(1)));
});

// Render serves every request through Cloudflare and then its own internal proxy, each appending to
// X-Forwarded-For. Walking the whole chain right to left and stopping at the first hop outside these
// networks yields the visitor's IP, and entries a client forges at the left end are never reached.
var trustedProxyNetworks = builder.Configuration.GetSection("TrustedProxyNetworks").Get<string[]>() ?? [];

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = null;
    foreach (var network in trustedProxyNetworks)
    {
        options.KnownIPNetworks.Add(IPNetwork.Parse(network));
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(supabaseConnection).UseSnakeCaseNamingConvention());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ChatLogger>();

builder.Services.AddHttpClient("gemini", c =>
{
    c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    c.DefaultRequestHeaders.Add("x-goog-api-key", geminiApiKey);
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok());

var geminiJsonOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

app.MapPost("/api/chat", async (ChatRequest request, ChatLogger chatLogger, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest();
    }

    const string model = "gemini-3.5-flash-lite";
    const string failureAnswer = "Something went wrong. Try again in a minute.";
    var previousInteractionId = string.IsNullOrWhiteSpace(request.PreviousInteractionId) ? null : request.PreviousInteractionId;
    var logContext = new ChatLogContext(request, previousInteractionId, model, Stopwatch.StartNew());

    int? geminiStatusCode = null;
    try
    {
        var client = httpClientFactory.CreateClient("gemini");

        var age = CalculateAge(birthDate);

        var systemInstruction = $"""
        You answer questions about Sixtus Ijoma for visitors to his personal site.
        Use only the profile below. If the profile does not say, find a graceful way to say that you don't have access to that information. Your personality is warm and eager to answer questions about Sixtus.
        Keep answers brief and warm: one to three sentences that fully answer the question. Lead with the direct answer and include only the details the question asks for. Don't list extra facts from the profile; the visitor can ask a follow-up for more. If asked to tell more about something, expand on what you just said instead of asking them to choose.
        When an answer covers more than one job, name which job each detail is from.
        The user is typing to you on a mock command line interface, so you cannot use any markdown or special styling. DO NOT answer any questions unrelated to Sixtus.
        Sixtus is {age} years old.
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
            await chatLogger.Log(logContext, failureAnswer, ChatStatus.GeminiError, geminiStatusCode, null);
            return Results.Ok(new { answer = failureAnswer });
        }

        var body = await response.Content.ReadAsStringAsync();
        using var parsed = JsonDocument.Parse(body);
        var json = parsed.RootElement;
        var interactionId = json.TryGetProperty("id", out var id) ? id.GetString() : null;

        string? answer = null;
        if (json.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
        {
            answer = steps.EnumerateArray()
                .Where(s => s.TryGetProperty("type", out var type) && type.GetString() == "model_output")
                .Where(s => s.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                .SelectMany(s => s.GetProperty("content").EnumerateArray())
                .Select(part => part.TryGetProperty("text", out var text) ? text.GetString() : null)
                .LastOrDefault(text => !string.IsNullOrWhiteSpace(text));
        }

        if (answer is null)
        {
            logger.LogWarning("Gemini returned no answer text for interaction {InteractionId}.", interactionId);
            await chatLogger.Log(logContext, failureAnswer, ChatStatus.GeminiError, geminiStatusCode, interactionId);
            return Results.Ok(new { answer = failureAnswer });
        }

        await chatLogger.Log(logContext, answer, ChatStatus.Ok, geminiStatusCode, interactionId);
        return Results.Ok(new { answer, interactionId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat request failed.");
        await chatLogger.Log(logContext, failureAnswer, ChatStatus.Exception, geminiStatusCode, null);
        return Results.Ok(new { answer = failureAnswer });
    }
});

app.Run();

static int CalculateAge(DateOnly birthDate)
{
    var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/New_York"));
    var age = today.Year - birthDate.Year;
    if (today < birthDate.AddYears(age))
    {
        age--;
    }

    return age;
}

public record ChatRequest(string Question, string? PreviousInteractionId, Guid? SessionId);
