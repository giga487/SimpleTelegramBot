using System.Text.Json;
using Microsoft.Extensions.Options;
using SimpleTelegramBot.Options;

namespace SimpleTelegramBot.Services;

public sealed class TelegramBotClient(
    HttpClient httpClient,
    IOptionsMonitor<TelegramOptions> options,
    ILogger<TelegramBotClient> logger) : ITelegramBotClient
{
    public async Task<TelegramSendResult> SendMessageAsync(string caller, string message, CancellationToken cancellationToken)
    {
        var currentOptions = options.CurrentValue;
        ValidateOptions(currentOptions);

        using var content = new FormUrlEncodedContent(BuildPayload(currentOptions, caller, message));
        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsync(BuildSendMessageUri(currentOptions), content, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new TelegramDeliveryException("Telegram API request failed.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TelegramDeliveryException("Telegram API request timed out.", exception);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Telegram API returned status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new TelegramDeliveryException($"Telegram API returned HTTP {(int)response.StatusCode}.");
        }

        return new TelegramSendResult(ReadMessageId(responseBody));
    }

    private static void ValidateOptions(TelegramOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            throw new TelegramConfigurationException("Telegram:BotToken is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ChatId))
        {
            throw new TelegramConfigurationException("Telegram:ChatId is not configured.");
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            throw new TelegramConfigurationException("Telegram:ApiBaseUrl must be an absolute URL.");
        }
    }

    private static Dictionary<string, string> BuildPayload(TelegramOptions options, string caller, string message)
    {
        var payload = new Dictionary<string, string>
        {
            ["chat_id"] = options.ChatId,
            ["text"] = $"[{caller}]{Environment.NewLine}{message}",
            ["disable_web_page_preview"] = options.DisableWebPagePreview ? "true" : "false"
        };

        if (!string.IsNullOrWhiteSpace(options.ParseMode))
        {
            payload["parse_mode"] = options.ParseMode;
        }

        return payload;
    }

    private static Uri BuildSendMessageUri(TelegramOptions options)
    {
        var baseUrl = options.ApiBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? options.ApiBaseUrl
            : $"{options.ApiBaseUrl}/";

        return new Uri(new Uri(baseUrl), $"bot{options.BotToken}/sendMessage");
    }

    private static int? ReadMessageId(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        if (document.RootElement.TryGetProperty("result", out var result) &&
            result.TryGetProperty("message_id", out var messageId) &&
            messageId.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }
}
