using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockQuoteAlert.Models;
using StockQuoteAlert.Services.Monitoring;

namespace StockQuoteAlert;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IStockMonitorService _monitorService;
    private readonly MonitorOptions _options;
    private readonly AppSettings _settings;

    public Worker(ILogger<Worker> logger,
                  IStockMonitorService monitorService,
                  MonitorOptions options,
                  IOptions<AppSettings> settings)
    {
        _logger = logger;
        _monitorService = monitorService;
        _options = options;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado. Monitorando {Symbol} a cada {Interval}s.",
            _options.Symbol, _settings.MonitoringIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _monitorService.CheckAndNotifyAsync(_options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao monitorar ação {Symbol}", _options.Symbol);
            }
            _logger.LogInformation("Ciclo finalizado. Aguardando {Seconds} segundos...", _settings.MonitoringIntervalSeconds);
            await Task.Delay(_settings.MonitoringIntervalSeconds * 1000, stoppingToken);
        }
    }
}
