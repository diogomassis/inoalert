namespace StockQuoteAlert.Services;

public class MarketStatusService : IMarketStatusService
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MarketStatusService> _logger;
    private static readonly TimeOnly MarketOpen = new(10, 0);
    private static readonly TimeOnly MarketClose = new(17, 30);
    private const string BrowserTimeZoneId = "E. South America Standard Time";

    public MarketStatusService(TimeProvider timeProvider, ILogger<MarketStatusService> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public bool IsMarketOpen()
    {
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
