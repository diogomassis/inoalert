namespace StockQuoteAlert.Services;

public interface INotificationStateManager
{
    bool ShouldNotify(string symbol, decimal currentPrice);
    void UpdateState(string symbol, decimal currentPrice);
}
