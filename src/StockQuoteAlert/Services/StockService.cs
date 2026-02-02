using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public class StockService : IStockService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StockService> _logger;
    private readonly AppSettings _settings;

    public StockService(HttpClient httpClient, ILogger<StockService> logger, IOptions<AppSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<decimal?> GetPriceAsync(string symbol)
    {
        try
        {
            var tokenParam = !string.IsNullOrWhiteSpace(_settings.BrapiToken) ? $"?token={_settings.BrapiToken}" : "";
            var response = await _httpClient.GetAsync($"https://brapi.dev/api/quote/{symbol}{tokenParam}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var stockData = JsonSerializer.Deserialize<StockResponse>(content);
            var price = stockData?.Results?.FirstOrDefault()?.Price;
            if (price == null)
            {
                _logger.LogWarning("Preço não encontrado para {Symbol}", symbol);
            }
            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter cotação para {Symbol}", symbol);
            throw;
        }
    }
}
