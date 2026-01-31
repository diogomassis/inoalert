using System.Text.Json.Serialization;

namespace StockQuoteAlert.Models;

public class StockResponse
{
    [JsonPropertyName("results")]
    public List<StockResult> Results { get; set; }
}

public class StockResult
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("regularMarketPrice")]
    public decimal Price { get; set; }
}
