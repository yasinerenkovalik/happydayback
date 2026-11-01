using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HappyDay.Application.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HappyDay.Persistance.Security;

public class JwtService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secret;
    private readonly int _expirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("JwtSettings");
        _secret = jwt["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret");
        _issuer = jwt["Issuer"] ?? throw new ArgumentNullException("JwtSettings:Issuer");
        _audience = jwt["Audience"] ?? throw new ArgumentNullException("JwtSettings:Audience");
        if (!int.TryParse(jwt["ExpirationInMinutes"], out _expirationMinutes))
            throw new ArgumentException("JwtSettings:ExpirationInMinutes geçersiz.");
    }

    /// <summary>
    /// Kısa ömürlü access token. "sid" claim'i oturum kaydına bağlar; oturum iptal edilince token
    /// süresi dolmadan geçersiz olur. Admin yalnızca platform girişinde verilir (AccountPrincipal.ForUser).
    /// </summary>
    public (string Token, DateTime ExpiresAt) CreateAccessToken(AccountPrincipal principal, Guid sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, principal.AccountId.ToString()), // nameid
            new(ClaimTypes.Role, principal.Role),
            new(SessionClaims.SessionId, sessionId.ToString()),
            new(SessionClaims.AccountType, principal.AccountType),
        };
        if (principal.CompanyId is { } companyId)
            claims.Add(new Claim("CompanyId", companyId.ToString()));

        var expires = DateTime.UtcNow.AddMinutes(_expirationMinutes);
        return (BuildToken(claims, expires), expires);
    }

    /// <summary>
    /// İmza, issuer, audience ve süreyi kontrol eder; geçersizse Principal null. Oturumun açık olup
    /// olmadığına bakmaz (o kontrol veritabanı ister, çağıran yapar).
    /// </summary>
    public (ClaimsPrincipal? Principal, bool Expired) Validate(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            return (new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _), false);
        }
        catch (SecurityTokenExpiredException)
        {
            return (null, true);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return (null, false);
        }
    }

    // Ortak token üretimi
    private string BuildToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}
