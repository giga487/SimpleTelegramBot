using Microsoft.Extensions.Options;
using SimpleTelegramBot.Models;
using SimpleTelegramBot.Options;

namespace SimpleTelegramBot.Services;

public sealed class InMemoryRequestMemoryStore(IOptionsMonitor<RequestMemoryOptions> options) : IRequestMemoryStore
{
    private readonly LinkedList<TelegramRequestEntry> _entries = [];
    private readonly object _sync = new();

    public void Add(TelegramRequestEntry entry)
    {
        lock (_sync)
        {
            _entries.AddFirst(entry);
            TrimToConfiguredLimit();
        }
    }

    public RequestHistorySnapshot GetLatest(int take, string? caller)
    {
        lock (_sync)
        {
            TrimToConfiguredLimit();

            var boundedTake = Math.Clamp(take, 1, CurrentMaxEntries);
            var normalizedCaller = NormalizeCaller(caller);
            var matching = _entries
                .Where(entry => normalizedCaller is null || string.Equals(entry.Caller, normalizedCaller, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var latest = matching.Take(boundedTake).ToArray();
            var groups = latest
                .GroupBy(entry => entry.Caller, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CallerRequestGroup
                {
                    Caller = group.Key,
                    Count = group.Count(),
                    Requests = group.ToArray()
                })
                .ToArray();

            return new RequestHistorySnapshot
            {
                Take = boundedTake,
                TotalStored = _entries.Count,
                MatchingStored = matching.Length,
                Requests = latest,
                Callers = groups
            };
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    private int CurrentMaxEntries => Math.Max(1, options.CurrentValue.MaxEntries);

    private void TrimToConfiguredLimit()
    {
        var maxEntries = CurrentMaxEntries;

        while (_entries.Count > maxEntries)
        {
            _entries.RemoveLast();
        }
    }

    private static string? NormalizeCaller(string? caller)
    {
        var normalized = caller?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
