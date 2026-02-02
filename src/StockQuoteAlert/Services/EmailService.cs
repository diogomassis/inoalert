using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public class EmailService : INotificationService
{
    private readonly AppSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<AppSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string title, string message)
    {
        try
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Stock Alert", _settings.Smtp.User));
            emailMessage.To.Add(new MailboxAddress("User", _settings.NotifyEmail));
            emailMessage.Subject = title;
            emailMessage.Body = new TextPart("plain")
            {
                Text = message
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(_settings.Smtp.User))
            {
                await client.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password);
            }
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
            _logger.LogInformation("E-mail enviado: {Subject}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail.");
        }
    }
}
