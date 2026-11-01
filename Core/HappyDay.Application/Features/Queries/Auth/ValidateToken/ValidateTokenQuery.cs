using System.Security.Claims;
using System.Text.Json.Serialization;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Interface.Repository;
using HappyDay.Persistance.Security;
using MediatR;

namespace HappyDay.Application.Features.Queries.Auth.ValidateToken;

/// <summary>GET api/users/validate-token — eski istemciler için: token geçerli mi, kime ait</summary>
public record ValidateTokenQuery(string? Token) : IRequest<ValidateTokenResponse>;

public class ValidateTokenResponse
{
    public bool IsValid { get; init; }
    public string? UserId { get; init; }
    public string? Role { get; init; }

    /// <summary>Geçersizse nedeni; controller 401 gövdesi olarak döner</summary>
    [JsonIgnore] public string? Message { get; init; }

    public static ValidateTokenResponse Invalid(string message) => new() { IsValid = false, Message = message };
}

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, ValidateTokenResponse>
{
    private readonly JwtService _jwt;
    private readonly IAuthSessionRepository _sessions;

    public ValidateTokenQueryHandler(JwtService jwt, IAuthSessionRepository sessions)
    {
        _jwt = jwt;
        _sessions = sessions;
    }

    public async Task<ValidateTokenResponse> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return ValidateTokenResponse.Invalid("Token geçersiz.");

        var (principal, expired) = _jwt.Validate(request.Token);
        if (expired) return ValidateTokenResponse.Invalid("Token süresi dolmuş.");
        if (principal is null) return ValidateTokenResponse.Invalid("Token geçersiz.");

        // İmzası geçerli olsa da oturumu kapatılmış token (çıkış, şifre değişikliği) geçersizdir
        if (SessionClaims.GetSessionId(principal) is not { } sessionId || !await _sessions.IsActiveAsync(sessionId))
            return ValidateTokenResponse.Invalid("Oturum sonlandırılmış.");

        return new ValidateTokenResponse
        {
            IsValid = true,
            UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            Role = principal.FindFirstValue(ClaimTypes.Role),
        };
    }
}
