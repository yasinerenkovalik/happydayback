using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using HappyDay.Application.Common.Email;

namespace HappyDay.Tests.Infrastructure;

public record SentEmail(string To, string Subject, string Body);

/// <summary>E-postaları göndermez, saklar; testler doğrulama ve sıfırlama bağlantılarını buradan okur</summary>
public sealed partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public Task SendAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
    {
        _sent.Enqueue(new SentEmail(to, subject, textBody ?? htmlBody));
        return Task.CompletedTask;
    }

    public IReadOnlyList<SentEmail> To(string email) => _sent.Where(m => m.To == email).ToList();

    /// <summary>Alıcıya gönderilen son e-postadaki /auth/{path} bağlantısının parametreleri</summary>
    public (Guid CompanyId, string Token) LastLink(string email, string path)
    {
        var body = To(email).Last(m => m.Body.Contains($"/auth/{path}")).Body;
        var match = LinkPattern().Match(body);
        Assert.True(match.Success, $"E-postada bağlantı yok: {body}");
        return (Guid.Parse(match.Groups["cid"].Value), match.Groups["token"].Value);
    }

    [GeneratedRegex(@"https://frontend\.test/auth/[a-z-]+\?cid=(?<cid>[0-9a-f-]{36})&token=(?<token>[A-Za-z0-9_-]+)")]
    private static partial Regex LinkPattern();
}
