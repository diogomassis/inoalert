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

var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
if (appSettings is null || !IsValidEmail(appSettings.NotifyEmail))
{
    Console.WriteLine($"[ERRO CRÍTICO] O email configurado '{appSettings?.NotifyEmail}' é inválido. A aplicação será encerrada para evitar falhas de envio.");
    Environment.Exit(1);
}

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(monitorOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMarketStatusService, MarketStatusService>();
builder.Services.AddSingleton<INotificationStateManager, NotificationStateManager>();

if (appSettings.EnabledChannels.Contains("Email", StringComparer.OrdinalIgnoreCase))
{
    builder.Services.AddTransient<INotificationService, EmailService>();
}
if (appSettings.EnabledChannels.Contains("Discord", StringComparer.OrdinalIgnoreCase))
{
    builder.Services.AddTransient<INotificationService, DiscordNotificationService>();
}

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

static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email)) return false;
    try
    {
        var addr = new MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
