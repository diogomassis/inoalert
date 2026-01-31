namespace StockQuoteAlert.Models;

public class AppSettings
{
    public SmtpSettings Smtp { get; set; }
    public string NotifyEmail { get; set; }
    public int MonitoringIntervalSeconds { get; set; } = 60;
    public string BrapiToken { get; set; }
}

public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}
