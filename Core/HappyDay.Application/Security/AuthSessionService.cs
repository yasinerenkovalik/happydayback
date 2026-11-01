using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HappyDay.Application.Interface.Repository;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HappyDay.Application.Common.Security;

/// <summary>Token'a yazılacak hesap bilgisi. Rol kuralları tek yerde: girişte ve yenilemede aynı sonucu verir.</summary>
public record AccountPrincipal(Guid AccountId, string AccountType, string Role, Guid? CompanyId)
{
    public const string CompanyAccount = "Company";
    public const string UserAccount = "User";

    public static AccountPrincipal ForCompany(Company company) =>
        new(company.Id, CompanyAccount, "Company", company.Id);

    /// <param name="allowAdmin">Admin rolü yalnızca platform girişinde (api/auth/login) verilir</param>
    public static AccountPrincipal ForUser(User user, bool allowAdmin)
    {
        if (allowAdmin && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            return new(user.Id, UserAccount, "Admin", null);

        // Firmaya bağlı kullanıcı firma yetkilisidir
        var companyId = user.CompanyId is { } id && id != Guid.Empty ? id : (Guid?)null;
        return new(user.Id, UserAccount, companyId.HasValue ? "Company" : "User", companyId);
    }
}

public class AuthTokens
{
    public required Guid SessionId { get; init; }
    public required AccountPrincipal Principal { get; init; }
    public required string AccessToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>Aynı anda gelen ikinci yenileme isteğinde null: tarayıcıda yeni refresh cookie'si zaten var</summary>
    public string? RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
}

/// <summary>Frontend'in oturum bilgisi; token'ın kendisini okuyamadığı için (httpOnly cookie) bu döner</summary>
public class AuthSessionInfo
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
    public Guid? CompanyId { get; set; }

    public static AuthSessionInfo From(AccountPrincipal p) => new() { UserId = p.AccountId, Role = p.Role, CompanyId = p.CompanyId };
}

public static class SessionClaims
{
    public const string SessionId = "sid";
    public const string AccountType = "acct";

    // JwtBearer gelen "sid" claim'ini ClaimTypes.Sid'e çevirir
    public static Guid? GetSessionId(ClaimsPrincipal user) =>
        Guid.TryParse((user.FindFirst(ClaimTypes.Sid) ?? user.FindFirst(SessionId))?.Value, out var id) ? id : null;
}

public interface IAuthSessionService
{
    Task<AuthTokens> StartAsync(AccountPrincipal principal);

    /// <summary>Refresh token'ı döndürür (rotation). Geçersiz, iptal edilmiş ya da tekrar kullanılmışsa null.</summary>
    Task<AuthTokens?> RefreshAsync(string refreshToken);

    Task RevokeAsync(string? refreshToken, Guid? sessionId, string reason);
}

public class AuthSessionService : IAuthSessionService
{
    // Aynı refresh token'la neredeyse aynı anda gelen iki istek (ör. iki sekme) hırsızlık sayılmasın
    private static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

    private readonly IAuthSessionRepository _sessions;
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly JwtService _jwt;
    private readonly IHttpContextAccessor _http;
    private readonly TimeSpan _refreshLifetime;

    public AuthSessionService(IAuthSessionRepository sessions, IUserRepository users, ICompanyRepository companies,
        JwtService jwt, IHttpContextAccessor http, IConfiguration config)
    {
        _sessions = sessions;
        _users = users;
        _companies = companies;
        _jwt = jwt;
        _http = http;
        _refreshLifetime = TimeSpan.FromDays(config.GetValue<int?>("Auth:RefreshTokenDays") ?? 14);
    }

    public async Task<AuthTokens> StartAsync(AccountPrincipal principal)
    {
        var now = DateTime.UtcNow;
        var secret = NewSecret();
        var request = _http.HttpContext?.Request;
        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            AccountId = principal.AccountId,
            AccountType = principal.AccountType,
            Role = principal.Role,
            RefreshTokenHash = Sha256(secret),
            CreatedAt = now,
            ExpiresAt = now + _refreshLifetime,
            UserAgent = Truncate(request?.Headers.UserAgent.ToString(), 256),
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        };
        await _sessions.AddAsync(session);

        return Issue(session, principal, Compose(session.Id, secret));
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken)
    {
        if (!TryParse(refreshToken, out var sessionId, out var secret)) return null;

        var now = DateTime.UtcNow;
        var session = await _sessions.GetAsync(sessionId);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now) return null;

        var hash = Sha256(secret);
        string? rotated = null;
        if (HashEquals(hash, session.RefreshTokenHash))
        {
            var newSecret = NewSecret();
            session.PreviousRefreshTokenHash = session.RefreshTokenHash;
            session.RefreshTokenHash = Sha256(newSecret);
            session.RotatedAt = now;
            rotated = Compose(session.Id, newSecret);
        }
        else if (!(HashEquals(hash, session.PreviousRefreshTokenHash) && session.RotatedAt > now - RotationGrace))
        {
            // Çoktan değiştirilmiş bir refresh token tekrar geldi: çalınmış olabilir, oturumu kapat
            await RevokeSessionAsync(session, "refresh_reuse");
            return null;
        }

        var principal = await LoadPrincipalAsync(session);
        if (principal is null)
        {
            await RevokeSessionAsync(session, "account_changed");
            return null;
        }

        session.ExpiresAt = now + _refreshLifetime;
        await _sessions.UpdateAsync(session);
        return Issue(session, principal, rotated);
    }

    public async Task RevokeAsync(string? refreshToken, Guid? sessionId, string reason)
    {
        AuthSession? session = null;
        if (refreshToken is not null && TryParse(refreshToken, out var id, out var secret))
        {
            var candidate = await _sessions.GetAsync(id);
            var hash = Sha256(secret);
            if (candidate is not null && (HashEquals(hash, candidate.RefreshTokenHash) || HashEquals(hash, candidate.PreviousRefreshTokenHash)))
                session = candidate;
        }
        if (session is null && sessionId is { } sid)
            session = await _sessions.GetAsync(sid);

        if (session is { RevokedAt: null })
            await RevokeSessionAsync(session, reason);
    }

    /// <summary>Hesap pasifleşmiş ya da rolü değişmişse null: oturum yeniden giriş ister</summary>
    private async Task<AccountPrincipal?> LoadPrincipalAsync(AuthSession session)
    {
        AccountPrincipal? principal = null;
        if (session.AccountType == AccountPrincipal.CompanyAccount)
        {
            var company = await _companies.GetByIdAsync(session.AccountId);
            if (company is not null) principal = AccountPrincipal.ForCompany(company);
        }
        else
        {
            var user = await _users.GetByIdAsync(session.AccountId);
            if (user is not null) principal = AccountPrincipal.ForUser(user, allowAdmin: session.Role == "Admin");
        }

        return principal?.Role == session.Role ? principal : null;
    }

    private Task RevokeSessionAsync(AuthSession session, string reason)
    {
        session.RevokedAt = DateTime.UtcNow;
        session.RevokeReason = reason;
        return _sessions.UpdateAsync(session);
    }

    private AuthTokens Issue(AuthSession session, AccountPrincipal principal, string? refreshToken)
    {
        var (accessToken, accessExpires) = _jwt.CreateAccessToken(principal, session.Id);
        return new AuthTokens
        {
            SessionId = session.Id,
            Principal = principal,
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpires,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = session.ExpiresAt,
        };
    }

    // Refresh token: "<oturum id>.<rastgele 32 bayt>"; veritabanında yalnızca rastgele kısmın SHA-256 özeti durur
    private static string Compose(Guid sessionId, string secret) => $"{sessionId:N}.{secret}";

    private static bool TryParse(string token, out Guid sessionId, out string secret)
    {
        var parts = token.Split('.', 2);
        secret = parts.Length == 2 ? parts[1] : string.Empty;
        return Guid.TryParseExact(parts[0], "N", out sessionId) && secret.Length > 0;
    }

    private static string NewSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static string Sha256(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private static bool HashEquals(string a, string? b) =>
        b is not null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];
}
