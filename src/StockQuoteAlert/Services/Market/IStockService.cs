namespace StockQuoteAlert.Services.Market;

public interface IStockService
{
    Task<decimal?> GetPriceAsync(string symbol);
}
