namespace SimpleTelegramBot.Models;

public sealed class SendTelegramMessageRequest
{
    public string? Caller { get; init; }

    public string? Message { get; init; }
}
