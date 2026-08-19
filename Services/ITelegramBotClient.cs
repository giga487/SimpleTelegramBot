namespace SimpleTelegramBot.Services;

public interface ITelegramBotClient
{
    Task<TelegramSendResult> SendMessageAsync(string caller, string message, CancellationToken cancellationToken);
}

public sealed record TelegramSendResult(int? MessageId);
