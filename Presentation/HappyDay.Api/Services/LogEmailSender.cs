using HappyDay.Application.Common.Email;

namespace HappyDay.Api.Services;

/// <summary>
/// Yalnızca geliştirme ortamı, SMTP ayarlı değilken: e-postayı göndermek yerine konsola yazar.
/// Doğrulama ve şifre sıfırlama bağlantıları buradan kopyalanıp denenebilir.
/// Bağlantılar gizli token içerdiği için üretimde kullanılmaz.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        _logger.LogWarning("SMTP ayarlı değil, e-posta gönderilmedi. Alıcı: {To} | Konu: {Subject} | {Text}",
            to, subject, textBody ?? htmlBody);
        return Task.CompletedTask;
    }
}
