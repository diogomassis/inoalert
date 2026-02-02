using Microsoft.Extensions.Logging;
using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public class StockMonitorService : IStockMonitorService
{
    private readonly IStockService _stockService;
    private readonly IEnumerable<INotificationService> _notificationServices; // Changed to Collection
    private readonly IMarketStatusService _marketStatusService;
    private readonly ILogger<StockMonitorService> _logger;

    // State Tracking
    private DateTime? _lastNotificationTime;
    private decimal? _lastNotifiedPrice;

    public StockMonitorService(
        IStockService stockService,
        IEnumerable<INotificationService> notificationServices,
        IMarketStatusService marketStatusService,
        ILogger<StockMonitorService> logger)
    {
        _stockService = stockService;
        _notificationServices = notificationServices;
        _marketStatusService = marketStatusService;
        _logger = logger;
    }

    public async Task CheckAndNotifyAsync(MonitorOptions options)
    {
        if (!_marketStatusService.IsMarketOpen())
        {
            _logger.LogInformation("Mercado fechado. Aguardando abertura...");
            return;
        }
        try
        {
            var price = await _stockService.GetPriceAsync(options.Symbol);
            if (!price.HasValue)
            {
                _logger.LogWarning("Não foi possível obter a cotação para {Symbol}", options.Symbol);
                return;
            }
            _logger.LogInformation("Cotação {Symbol}: {Price}", options.Symbol, price);
            if (price.Value > options.SellPrice)
            {
                if (ShouldNotify(price.Value))
                {
                    var msg = $"Aconselhamos a VENDA de {options.Symbol}.\nPreço atual: {price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}\nAlvo de venda: {options.SellPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    _logger.LogWarning("ALERTA DE VENDA: {Msg}", msg);
                    foreach (var channel in _notificationServices)
                    {
                        await channel.SendNotificationAsync($"[VENDA] Alerta para {options.Symbol}", msg);
                    }
                    UpdateState(price.Value);
                }
            }
            else if (price.Value < options.BuyPrice)
            {
                if (ShouldNotify(price.Value))
                {
                    var msg = $"Aconselhamos a COMPRA de {options.Symbol}.\nPreço atual: {price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}\nAlvo de compra: {options.BuyPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    _logger.LogWarning("ALERTA DE COMPRA: {Msg}", msg);
                    foreach (var channel in _notificationServices)
                    {
                        await channel.SendNotificationAsync($"[COMPRA] Alerta para {options.Symbol}", msg);
                    }
                    UpdateState(price.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante a verificação de monitoramento.");
            throw;
        }
    }

    private bool ShouldNotify(decimal currentPrice)
    {
        if (!_lastNotificationTime.HasValue || !_lastNotifiedPrice.HasValue)
        {
            return true;
        }
        if (_lastNotifiedPrice.Value != currentPrice)
        {
            _logger.LogWarning("Preço mudou desde último alerta ({Old} -> {New}). Notificando.", _lastNotifiedPrice, currentPrice);
            return true;
        }
        if ((DateTime.UtcNow - _lastNotificationTime.Value).TotalMinutes > 10)
        {
            _logger.LogWarning("Mais de 10 minutos desde o último alerta. Enviando lembrete.");
            return true;
        }
        _logger.LogWarning("Notificação ignorada (Cache Hit). Preço inalterado e menos de 10 min.");
        return false;
    }

    private void UpdateState(decimal price)
    {
        _lastNotificationTime = DateTime.UtcNow;
        _lastNotifiedPrice = price;
    }
}
