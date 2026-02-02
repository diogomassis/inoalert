using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services.Market;

public class MarketStatusService : IMarketStatusService
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MarketStatusService> _logger;
    private readonly AppSettings _settings;
    private static readonly TimeOnly MarketOpen = new(10, 0);
    private static readonly TimeOnly MarketClose = new(17, 30);
    private const string BrowserTimeZoneId = "E. South America Standard Time";

    public MarketStatusService(
        TimeProvider timeProvider,
        ILogger<MarketStatusService> logger,
        IOptions<AppSettings> settings)
    {
        _timeProvider = timeProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    public bool IsMarketOpen()
    {
        if (_settings.IgnoreMarketHours)
        {
            _logger.LogWarning("Monitoramento forçado (IgnoreMarketHours=true). Ignorando horário do mercado.");
            return true;
        }
        var utcNow = _timeProvider.GetUtcNow();
        var brasiliaTime = utcNow.ToOffset(TimeSpan.FromHours(-3));
        if (brasiliaTime.DayOfWeek == DayOfWeek.Saturday || brasiliaTime.DayOfWeek == DayOfWeek.Sunday)
        {
            _logger.LogDebug($"Mercado fechado: {brasiliaTime.DayOfWeek}");
            return false;
        }
        var timeOfDay = TimeOnly.FromTimeSpan(brasiliaTime.TimeOfDay);
        if (timeOfDay >= MarketOpen && timeOfDay <= MarketClose)
        {
            return true;
        }
        _logger.LogDebug($"Mercado fechado. Hora atual: {timeOfDay}. Aberto entre {MarketOpen} e {MarketClose}");
        return false;
    }
}
