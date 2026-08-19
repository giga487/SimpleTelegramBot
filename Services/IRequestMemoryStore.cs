using SimpleTelegramBot.Models;

namespace SimpleTelegramBot.Services;

public interface IRequestMemoryStore
{
    void Add(TelegramRequestEntry entry);

    RequestHistorySnapshot GetLatest(int take, string? caller);

    void Clear();
}
