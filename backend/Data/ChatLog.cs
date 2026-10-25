namespace backend.Data;

public enum ChatStatus
{
    Ok,
    GeminiError,
    Exception
}

public class ChatLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }

    public string Question { get; set; } = "";

    public string Answer { get; set; } = "";

    public ChatStatus Status { get; set; }

    public int? GeminiStatusCode { get; set; }

    public string? InteractionId { get; set; }

    public string? PreviousInteractionId { get; set; }

    public string Model { get; set; } = "";

    // milliseconds
    public int Latency { get; set; }

    public string? IpHash { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
