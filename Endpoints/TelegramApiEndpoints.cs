using Microsoft.AspNetCore.Mvc;
using SimpleTelegramBot.Models;
using SimpleTelegramBot.Services;

namespace SimpleTelegramBot.Endpoints;

public static class TelegramApiEndpoints
{
    private const int DefaultTake = 20;
    private const int MaxCallerLength = 120;
    private const int MaxMessageLength = 4096;

    public static IEndpointRouteBuilder MapTelegramApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/telegram")
            .WithTags("Telegram");

        api.MapPost("/messages", SendMessageAsync)
            .WithName("SendTelegramMessage");

        api.MapGet("/requests", GetRequests)
            .WithName("GetTelegramRequests");

        api.MapDelete("/requests", ClearRequests)
            .WithName("ClearTelegramRequests");

        return endpoints;
    }

    private static async Task<IResult> SendMessageAsync(
        SendTelegramMessageRequest request,
        ITelegramBotClient telegramBotClient,
        IRequestMemoryStore requestMemoryStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var logger = loggerFactory.CreateLogger("TelegramApi");
        var caller = request.Caller!.Trim();
        var message = request.Message!.Trim();
        var requestId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.UtcNow;

        try
        {
            var result = await telegramBotClient.SendMessageAsync(caller, message, cancellationToken);
            var entry = CreateEntry(requestId, requestedAt, caller, message, sent: true, result.MessageId, error: null);
            requestMemoryStore.Add(entry);

            logger.LogInformation(
                "Telegram request {RequestId} sent for caller {Caller} with Telegram message id {TelegramMessageId}.",
                requestId,
                caller,
                result.MessageId);

            return Results.Ok(new SendTelegramMessageResponse
            {
                RequestId = requestId,
                RequestedAt = requestedAt,
                Caller = caller,
                Sent = true,
                TelegramMessageId = result.MessageId
            });
        }
        catch (TelegramConfigurationException exception)
        {
            requestMemoryStore.Add(CreateEntry(requestId, requestedAt, caller, message, sent: false, telegramMessageId: null, exception.Message));
            logger.LogError(exception, "Telegram request {RequestId} for caller {Caller} failed because Telegram is not configured.", requestId, caller);

            return Results.Problem(
                title: "Telegram is not configured.",
                detail: exception.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (TelegramDeliveryException exception)
        {
            requestMemoryStore.Add(CreateEntry(requestId, requestedAt, caller, message, sent: false, telegramMessageId: null, exception.Message));
            logger.LogWarning(exception, "Telegram request {RequestId} for caller {Caller} failed during delivery.", requestId, caller);

            return Results.Problem(
                title: "Telegram delivery failed.",
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static IResult GetRequests(
        [FromServices] IRequestMemoryStore requestMemoryStore,
        [FromQuery] int? take,
        [FromQuery] string? caller)
    {
        return Results.Ok(requestMemoryStore.GetLatest(take ?? DefaultTake, caller));
    }

    private static IResult ClearRequests(
        [FromServices] IRequestMemoryStore requestMemoryStore,
        [FromServices] ILoggerFactory loggerFactory)
    {
        requestMemoryStore.Clear();
        loggerFactory.CreateLogger("TelegramApi").LogInformation("Telegram request memory was cleared.");
        return Results.NoContent();
    }

    private static Dictionary<string, string[]> Validate(SendTelegramMessageRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Caller))
        {
            errors[nameof(request.Caller)] = ["Caller is required."];
        }
        else if (request.Caller.Trim().Length > MaxCallerLength)
        {
            errors[nameof(request.Caller)] = [$"Caller must be at most {MaxCallerLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            errors[nameof(request.Message)] = ["Message is required."];
        }
        else if (request.Message.Trim().Length > MaxMessageLength)
        {
            errors[nameof(request.Message)] = [$"Message must be at most {MaxMessageLength} characters."];
        }

        return errors;
    }

    private static TelegramRequestEntry CreateEntry(
        Guid requestId,
        DateTimeOffset requestedAt,
        string caller,
        string message,
        bool sent,
        int? telegramMessageId,
        string? error)
    {
        return new TelegramRequestEntry
        {
            Id = requestId,
            RequestedAt = requestedAt,
            Caller = caller,
            Message = message,
            Sent = sent,
            TelegramMessageId = telegramMessageId,
            Error = error
        };
    }
}
