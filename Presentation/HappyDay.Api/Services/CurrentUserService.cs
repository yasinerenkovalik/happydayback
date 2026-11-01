using System.Security.Claims;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Interface.Services;

namespace HappyDay.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? UserId =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : null;

    // CompanyId istemciden alınmaz; yalnızca JWT claim'inden okunur.
    public Guid? CompanyId =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirst("CompanyId")?.Value, out var companyId)
            ? companyId
            : null;

    public Guid? SessionId =>
        _httpContextAccessor.HttpContext is { } context ? SessionClaims.GetSessionId(context.User) : null;

    public string? AccountType => _httpContextAccessor.HttpContext?.User.FindFirst(SessionClaims.AccountType)?.Value;

    public bool CanManageCompany(Guid? companyId) =>
        IsAdmin || (companyId.HasValue && CompanyId.HasValue && companyId.Value == CompanyId.Value);
}
