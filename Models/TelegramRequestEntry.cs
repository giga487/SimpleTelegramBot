namespace SimpleTelegramBot.Models;

public sealed class TelegramRequestEntry
{
    public Guid Id { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public string Caller { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool Sent { get; init; }

    public int? TelegramMessageId { get; init; }

    public string? Error { get; init; }
}
