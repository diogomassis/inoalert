using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockQuoteAlert.Models;

namespace StockQuoteAlert.Services;

public class EmailService : IEmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<AppSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Stock Alert", _settings.Smtp.User));
            message.To.Add(new MailboxAddress("User", _settings.NotifyEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(_settings.Smtp.User))
            {
                await client.AuthenticateAsync(_settings.Smtp.User, _settings.Smtp.Password);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("E-mail enviado: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail.");
        }
    }
}
