namespace StockQuoteAlert.Services;

public interface IStockService
{
    Task<decimal?> GetPriceAsync(string symbol);
}
