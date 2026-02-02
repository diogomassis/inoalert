namespace StockQuoteAlert.Services.Notifications;

public class DiscordNotificationService : INotificationService
{
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(ILogger<DiscordNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendNotificationAsync(string title, string message)
    {
        _logger.LogInformation("[Discord] Sending notification to Discord Channel...", title);
        _logger.LogInformation("[Discord] Title: {Title}", title);
        _logger.LogInformation("[Discord] Message: {Message}", message);
        return Task.CompletedTask;
    }
}
