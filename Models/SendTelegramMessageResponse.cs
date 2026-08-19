namespace SimpleTelegramBot.Models;

public sealed class SendTelegramMessageResponse
{
    public Guid RequestId { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public string Caller { get; init; } = string.Empty;

    public bool Sent { get; init; }

    public int? TelegramMessageId { get; init; }
}
