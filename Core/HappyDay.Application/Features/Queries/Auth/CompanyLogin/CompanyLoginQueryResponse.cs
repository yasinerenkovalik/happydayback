using System.Text.Json.Serialization;
using HappyDay.Application.Common.Security;
namespace HappyDay.Application.Features.Queries.Auth.OrganizationLogin;

public class CompanyLoginQueryResponse
{
    public string Token { get; set; }
    public bool IsEmailConfirmed { get; set; }

    /// <summary>Controller bunlardan httpOnly cookie yazar; yanıt gövdesine girmez</summary>
    [JsonIgnore] public AuthTokens? Tokens { get; set; }
}