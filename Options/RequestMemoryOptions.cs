namespace SimpleTelegramBot.Options;

public sealed class RequestMemoryOptions
{
    public const string SectionName = "RequestMemory";

    public int MaxEntries { get; init; } = 500;
}
