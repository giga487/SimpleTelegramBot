namespace SimpleTelegramBot.Services;

public sealed class TelegramConfigurationException(string message) : InvalidOperationException(message);

public sealed class TelegramDeliveryException : InvalidOperationException
{
    public TelegramDeliveryException(string message)
        : base(message)
    {
    }

    public TelegramDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
