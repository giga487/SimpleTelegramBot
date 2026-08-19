namespace SimpleTelegramBot.Options;

public sealed class ApiCorsOptions
{
    public const string SectionName = "ApiCors";
    public const string PolicyName = "ConfiguredExternalCallers";

    public string[] AllowedOrigins { get; init; } = [];
}
