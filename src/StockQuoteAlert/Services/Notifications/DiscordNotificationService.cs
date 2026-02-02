using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockQuoteAlert.Models;
using System.Net.Http.Json;

namespace StockQuoteAlert.Services.Notifications;

public class DiscordNotificationService : INotificationService
{
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;

    public DiscordNotificationService(ILogger<DiscordNotificationService> logger, IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _appSettings = appSettings.Value;
    }

    public async Task SendNotificationAsync(string title, string message)
    {
        _logger.LogInformation("[Discord] Sending notification to Discord Channel...");
        if (string.IsNullOrWhiteSpace(_appSettings.DiscordWebhookUrl))
        {
            _logger.LogWarning("[Discord] Webhook URL not configured. Skipping notification.");
            return;
        }
        var payload = new
        {
            content = $"**{title}**\n{message}"
        };
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_appSettings.DiscordWebhookUrl, payload);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("[Discord] Notification sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Discord] Failed to send notification.");
        }
    }
}
