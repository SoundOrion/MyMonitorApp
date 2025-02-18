using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyMonitorApp.Services;

public class EmailNotificationService : INotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string subject, string message)
    {
        try
        {
            using (SmtpClient client = new SmtpClient("smtp.example.com", 587))
            {
                client.Credentials = new System.Net.NetworkCredential("your_email@example.com", "your_password");
                client.EnableSsl = true;

                MailMessage mail = new MailMessage
                {
                    From = new MailAddress("alert@example.com"),
                    Subject = subject,
                    Body = message
                };
                mail.To.Add("admin@example.com");

                await client.SendMailAsync(mail);
                _logger.LogInformation("メール通知を送信しました。");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"メール通知の送信に失敗: {ex.Message}");
        }
    }
}

