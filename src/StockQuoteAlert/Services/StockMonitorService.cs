using Microsoft.Extensions.Logging;
using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public class StockMonitorService : IStockMonitorService
{
    private readonly IStockService _stockService;
    private readonly IEmailService _emailService;
    private readonly ILogger<StockMonitorService> _logger;

    public StockMonitorService(IStockService stockService, IEmailService emailService, ILogger<StockMonitorService> logger)
    {
        _stockService = stockService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task CheckAndNotifyAsync(MonitorOptions options)
    {
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
                var msg = $"Aconselhamos a VENDA de {options.Symbol}.\nPreço atual: {price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}\nAlvo de venda: {options.SellPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                _logger.LogWarning("ALERTA DE VENDA: {Msg}", msg);
                await _emailService.SendEmailAsync($"[VENDA] Alerta para {options.Symbol}", msg);
            }
            else if (price.Value < options.BuyPrice)
            {
                var msg = $"Aconselhamos a COMPRA de {options.Symbol}.\nPreço atual: {price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}\nAlvo de compra: {options.BuyPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                _logger.LogWarning("ALERTA DE COMPRA: {Msg}", msg);
                await _emailService.SendEmailAsync($"[COMPRA] Alerta para {options.Symbol}", msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante a verificação de monitoramento.");
            throw;
        }
    }
}
