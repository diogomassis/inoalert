using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services.Monitoring;

public interface IStockMonitorService
{
    Task CheckAndNotifyAsync(MonitorOptions options);
}
