using HappyDay.Api.Auth;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Features.Commands.Auth.ChangePassword;
using HappyDay.Application.Features.Queries.Auth.PlatformLogin;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller;

/// <summary>
/// Platform genel giriş noktası ve oturum yönetimi. Token'lar httpOnly cookie'lerde taşınır;
/// yanıt gövdesindeki token yalnızca Swagger gibi cookie kullanmayan istemciler için.
/// </summary>
[Route("api/auth")]
[ApiController]
public class PlatformAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthSessionService _sessions;
    private readonly ICurrentUserService _currentUser;

    public PlatformAuthController(IMediator mediator, IAuthSessionService sessions, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _sessions = sessions;
        _currentUser = currentUser;
    }

    [EnableRateLimiting("Auth")]
    [HttpPost("login")]
    public async Task<GeneralResponse<PlatformLoginQueryResponse>> Login(
        [FromForm] PlatformLoginQueryRequest request)
    {
        var res = await _mediator.Send(request);
        if (res.isSuccess && res.Data?.Tokens is { } tokens) AuthCookies.Write(HttpContext, tokens);
        return res;
    }

    /// <summary>Refresh cookie'siyle yeni access token alır; refresh token da her seferinde değişir.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("Refresh")]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!AuthCookies.HasClientHeader(Request))
            return BadRequest(Fail<AuthSessionInfo>("Geçersiz istek."));

        var refreshToken = Request.Cookies[AuthCookies.RefreshToken];
        var tokens = string.IsNullOrEmpty(refreshToken) ? null : await _sessions.RefreshAsync(refreshToken);
        if (tokens is null)
        {
            AuthCookies.Clear(HttpContext);
            return Unauthorized(Fail<AuthSessionInfo>("Oturumunuz sona erdi, lütfen tekrar giriş yapın."));
        }

        AuthCookies.Write(HttpContext, tokens);
        return Ok(new GeneralResponse<AuthSessionInfo> { isSuccess = true, Data = AuthSessionInfo.From(tokens.Principal) });
    }

    /// <summary>Oturumdaki hesap: frontend token'ı okuyamadığı için kim olduğunu buradan öğrenir.</summary>
    [Authorize]
    [HttpGet("session")]
    public GeneralResponse<AuthSessionInfo> Session() => new()
    {
        isSuccess = true,
        Data = new AuthSessionInfo
        {
            UserId = _currentUser.UserId ?? Guid.Empty,
            Role = _currentUser.Role ?? "User",
            CompanyId = _currentUser.CompanyId,
        }
    };

    /// <summary>Oturumu sunucuda kapatır ve cookie'leri siler; çalınmış bir access token da hemen geçersiz olur.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!AuthCookies.HasClientHeader(Request))
            return BadRequest(Fail<object>("Geçersiz istek."));

        await _sessions.RevokeAsync(Request.Cookies[AuthCookies.RefreshToken], _currentUser.SessionId, "logout");
        AuthCookies.Clear(HttpContext);
        return Ok(new GeneralResponse<object> { isSuccess = true, Message = "Çıkış yapıldı." });
    }

    [Authorize]
    [EnableRateLimiting("Auth")]
    [HttpPost("change-password")]
    public async Task<GeneralResponse<Unit>> ChangePassword([FromBody] ChangePasswordCommand command) =>
        await _mediator.Send(command);

    private static GeneralResponse<T> Fail<T>(string message) where T : new() => new() { isSuccess = false, Message = message };
}
