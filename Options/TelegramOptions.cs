namespace SimpleTelegramBot.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;

    public string ChatId { get; init; } = string.Empty;

    public string ApiBaseUrl { get; init; } = "https://api.telegram.org";

    public string? ParseMode { get; init; }

    public bool DisableWebPagePreview { get; init; } = true;
}
