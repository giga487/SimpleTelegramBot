namespace SimpleTelegramBot.Models;

public sealed class RequestHistorySnapshot
{
    public int Take { get; init; }

    public int TotalStored { get; init; }

    public int MatchingStored { get; init; }

    public IReadOnlyList<TelegramRequestEntry> Requests { get; init; } = [];

    public IReadOnlyList<CallerRequestGroup> Callers { get; init; } = [];
}

public sealed class CallerRequestGroup
{
    public string Caller { get; init; } = string.Empty;

    public int Count { get; init; }

    public IReadOnlyList<TelegramRequestEntry> Requests { get; init; } = [];
}
