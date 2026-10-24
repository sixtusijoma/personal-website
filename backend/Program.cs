using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var geminiApiKey = builder.Configuration["GEMINI_API_KEY"];
var profile = builder.Configuration["PROFILE_JSON"];
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

app.MapPost("/api/chat", async (ChatRequest request)  =>
{
    if (string.IsNullOrWhiteSpace(profile))
    {
        return Results.Ok(new { answer = "Profile is not configured." });
    }

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
        ? new { model = "gemini-3.5-flash-lite", input = request.Question, system_instruction = systemInstruction }
        : new { model = "gemini-3.5-flash-lite", input = request.Question, previous_interaction_id = request.PreviousInteractionId, system_instruction = systemInstruction };

    var response = await client.PostAsJsonAsync(
        "https://generativelanguage.googleapis.com/v1beta/interactions",
        payload);

    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return Results.Ok(new { answer = $"Gemini returned {(int)response.StatusCode}. Try again in a minute." });
    }

    using var parsed = JsonDocument.Parse(body);
    var json = parsed.RootElement;
    var interactionId = json.TryGetProperty("id", out var id) ? id.GetString() : null;

    var answer = json.GetProperty("steps").EnumerateArray()
        .Last(s => s.GetProperty("type").GetString() == "model_output").
        GetProperty("content")[0].GetProperty("text").GetString() ?? "";

    return Results.Ok(new { answer, interactionId });
});
app.UseCors();



app.Run();

record ChatRequest(string Question, string? PreviousInteractionId);



// after app.UseHttpsRedirection()




record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

