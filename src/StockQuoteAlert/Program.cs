using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using StockQuoteAlert;
using StockQuoteAlert.Models;
using StockQuoteAlert.Services;

if (args.Length < 3)
{
    Console.WriteLine("Uso: stock-quote-alert <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>");
    Console.WriteLine("Exemplo: stock-quote-alert PETR4 22.67 22.59");
    return;
}

var symbol = args[0].ToUpper();
if (!decimal.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var sellPrice))
{
    Console.WriteLine($"Preço de venda inválido: {args[1]}");
    return;
}
if (!decimal.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var buyPrice))
{
    Console.WriteLine($"Preço de compra inválido: {args[2]}");
    return;
}

var monitorOptions = new MonitorOptions(symbol, sellPrice, buyPrice);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(monitorOptions);
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IStockMonitorService, StockMonitorService>();
builder.Services.AddHttpClient<IStockService, StockService>()
    .AddPolicyHandler(GetRetryPolicy());
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
