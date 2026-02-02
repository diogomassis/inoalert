namespace StockQuoteAlert.Services;

public class DiscordNotificationService : INotificationService
{
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(ILogger<DiscordNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendNotificationAsync(string title, string message)
    {
        _logger.LogInformation("[DISCORD MOCK] Sending notification to Discord Channel...");
        _logger.LogInformation("[DISCORD MOCK] Title: {Title}", title);
        _logger.LogInformation("[DISCORD MOCK] Message: {Message}", message);
        return Task.CompletedTask;
    }
}
