using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public interface IStockMonitorService
{
    Task CheckAndNotifyAsync(MonitorOptions options);
}
