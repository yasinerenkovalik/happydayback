// Presentation/HappyDay.Api/Services/Mail/MailService.cs
using System.Threading;
using System.Threading.Tasks;
using HappyDay.Application.Common.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HappyDay.Api.Services.Mail
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Mutlu Günüm";

        /// <summary>
        /// Sunucu sertifikasının iptal edilip edilmediği (CRL/OCSP) kontrol edilsin mi. Zincir ve alan adı
        /// doğrulaması her durumda yapılır. macOS'te .NET, OCSP adresi olmayan sertifikalarda (ör. Gmail)
        /// bu kontrolü tamamlayamayıp bağlantıyı reddedebiliyor; geliştirmede kapatılır, sunucuda açık kalır.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;
    }

    public class MailService : IEmailSender
    {
        private readonly SmtpOptions _opt;
        public MailService(IOptions<SmtpOptions> opt) => _opt = opt.Value;

        public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody ?? "Bu e-postanın HTML sürümünü görüntülemek daha iyi bir deneyim sağlar."
            };
            msg.Body = builder.ToMessageBody();

            using var client = new SmtpClient { Timeout = 15000, CheckCertificateRevocation = _opt.CheckCertificateRevocation };
            try
            {
                await client.ConnectAsync(_opt.Host, _opt.Port, _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
                if (!string.IsNullOrEmpty(_opt.Username))
                    await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);
                await client.SendAsync(msg, ct);
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true, ct);
            }
        }
    }
}
