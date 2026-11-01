using System.Text.Json.Serialization;
using HappyDay.Application.Common.Security;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.Auth.PlatformLogin;

/// <summary>
/// Tek giriş noktası: hem kullanıcı (admin/user) hem firma hesaplarını kabul eder.
/// </summary>
public class PlatformLoginQueryRequest : IRequest<GeneralResponse<PlatformLoginQueryResponse>>
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class PlatformLoginQueryResponse
{
    public string Token { get; set; } = default!;
    public string Role { get; set; } = default!;

    /// <summary>Controller bunlardan httpOnly cookie yazar; yanıt gövdesine girmez</summary>
    [JsonIgnore] public AuthTokens? Tokens { get; set; }
}
