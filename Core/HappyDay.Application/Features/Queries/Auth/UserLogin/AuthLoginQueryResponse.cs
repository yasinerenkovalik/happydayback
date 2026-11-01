using System.Text.Json.Serialization;
using HappyDay.Application.Common.Security;
namespace HappyDay.Application.Features.Queries.AutLogin;

public class AuthLoginQueryResponse
{
    public string Token { get; set; }

    /// <summary>Controller bunlardan httpOnly cookie yazar; yanıt gövdesine girmez</summary>
    [JsonIgnore] public AuthTokens? Tokens { get; set; }
}