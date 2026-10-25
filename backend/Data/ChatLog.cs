namespace backend.Data;

public enum ChatStatus
{
    Ok,
    GeminiError,
    ProfileMissing,
    Exception
}

public class ChatLog
{
    // Primary key.
    public Guid Id { get; set; } = Guid.NewGuid();

    // One id per page load, sent by the frontend (Guid.Empty if the client did not send one).
    public Guid SessionId { get; set; }

    // The visitor's question as typed.
    public string Question { get; set; } = "";

    // The answer returned to the visitor (including error/fallback text).
    public string Answer { get; set; } = "";

    // Outcome of the request; stored as a string column.
    public ChatStatus Status { get; set; }

    // HTTP status code from Gemini, when a call was made.
    public int? GeminiStatusCode { get; set; }

    // Gemini interaction id returned for this turn.
    public string? InteractionId { get; set; }

    // Gemini interaction id the client sent to continue a conversation.
    public string? PreviousInteractionId { get; set; }

    // Gemini model name used for the request.
    public string Model { get; set; } = "";

    // Total time spent handling the request, in milliseconds.
    public int LatencyMs { get; set; }

    // Raw client IP address (personal data; rows are purged after 90 days).
    public string? IpAddress { get; set; }

    // Client User-Agent header.
    public string? UserAgent { get; set; }

    // When the row was written (UTC).
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
