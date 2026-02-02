namespace StockQuoteAlert.Services;

public class NotificationStateManager : INotificationStateManager
{
    private readonly ConcurrentDictionary<string, StockState> _states = new();
    private readonly ILogger<NotificationStateManager> _logger;

    public NotificationStateManager(ILogger<NotificationStateManager> logger)
    {
        _logger = logger;
    }

    public bool ShouldNotify(string symbol, decimal currentPrice)
    {
        if (!_states.TryGetValue(symbol, out var state))
        {
            return true;
        }
        if (state.LastNotifiedPrice != currentPrice)
        {
            _logger.LogWarning("[{Symbol}] Preço mudou desde último alerta ({Old} -> {New}). Notificando.",
                symbol, state.LastNotifiedPrice, currentPrice);
            return true;
        }
        if ((DateTime.UtcNow - state.LastNotificationTime).TotalMinutes > 10)
        {
            _logger.LogWarning("[{Symbol}] Mais de 10 minutos desde o último alerta. Enviando lembrete.", symbol);
            return true;
        }
        _logger.LogWarning("[{Symbol}] Notificação ignorada (Cache Hit). Preço inalterado e menos de 10 min.", symbol);
        return false;
    }

    public void UpdateState(string symbol, decimal currentPrice)
    {
        var newState = new StockState
        {
            LastNotificationTime = DateTime.UtcNow,
            LastNotifiedPrice = currentPrice
        };

        _states.AddOrUpdate(symbol, newState, (key, oldValue) => newState);
    }

    private class StockState
    {
        public DateTime LastNotificationTime { get; set; }
        public decimal LastNotifiedPrice { get; set; }
    }
}
