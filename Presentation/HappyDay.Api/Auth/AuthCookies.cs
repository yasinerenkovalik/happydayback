using HappyDay.Application.Common.Security;

namespace HappyDay.Api.Auth;

/// <summary>
/// Oturum cookie'leri. Token'lar JavaScript'in okuyamadığı (httpOnly) cookie'lerde durur; bir XSS açığı
/// token'ı çalıp başka yerde kullanamaz. Refresh token yalnızca /api/auth altındaki isteklere gider.
/// </summary>
public static class AuthCookies
{
    public const string AccessToken = "davetly.at";
    public const string RefreshToken = "davetly.rt";

    /// <summary>JS'in okuyabildiği, sır içermeyen işaret: frontend oturum yokken boşuna istek atmasın diye</summary>
    public const string SessionHint = "davetly.session";

    public const string RefreshPath = "/api/auth";

    /// <summary>
    /// Cookie ile kimliği doğrulanan ve veri değiştiren isteklerde zorunlu başlık (CSRF koruması).
    /// Başka bir siteden gönderilen form bu başlığı ekleyemez; fetch ile eklerse CORS ön kontrolüne takılır.
    /// </summary>
    public const string ClientHeader = "X-Davetly-Client";

    /// <summary>HttpContext.Items anahtarı: bu istekte token cookie'den mi okundu</summary>
    public const string FromCookieItem = "auth:from-cookie";

    public static bool HasClientHeader(HttpRequest request) => request.Headers.ContainsKey(ClientHeader);

    public static void Write(HttpContext context, AuthTokens tokens)
    {
        var cookies = context.Response.Cookies;
        cookies.Append(AccessToken, tokens.AccessToken, Options(context, httpOnly: true, path: "/", tokens.AccessTokenExpiresAt));
        if (tokens.RefreshToken is not null)
            cookies.Append(RefreshToken, tokens.RefreshToken, Options(context, httpOnly: true, RefreshPath, tokens.RefreshTokenExpiresAt));
        cookies.Append(SessionHint, "1", Options(context, httpOnly: false, path: "/", tokens.RefreshTokenExpiresAt, siteWide: true));
    }

    public static void Clear(HttpContext context)
    {
        var cookies = context.Response.Cookies;
        cookies.Delete(AccessToken, Options(context, httpOnly: true, path: "/", null));
        cookies.Delete(RefreshToken, Options(context, httpOnly: true, RefreshPath, null));
        cookies.Delete(SessionHint, Options(context, httpOnly: false, path: "/", null, siteWide: true));
    }

    /// <param name="siteWide">
    /// Yalnızca işaret cookie'si için: frontend (mutlugunum.com) API'den (api.mutlugunum.com) ayrı bir alt alan
    /// adındaysa, frontend'in okuyabilmesi için ortak alan adına (Auth:SiteDomain) yazılır. Token cookie'leri yalnızca
    /// API'nin kendi alan adında kalır.
    /// </param>
    private static CookieOptions Options(HttpContext context, bool httpOnly, string path, DateTime? expires, bool siteWide = false)
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

        // Strict: frontend ve API aynı sitedeyse (ör. davetly.com ve api.davetly.com).
        // Farklı sitelerdeyse Auth:CookieSameSite=None; CSRF'i o durumda ClientHeader + CORS korur.
        var sameSite = Enum.TryParse<SameSiteMode>(config["Auth:CookieSameSite"], ignoreCase: true, out var mode) ? mode : SameSiteMode.Strict;

        return new CookieOptions
        {
            HttpOnly = httpOnly,
            // Geliştirmede http://localhost; diğer her yerde yalnızca HTTPS
            Secure = !env.IsDevelopment() || context.Request.IsHttps || sameSite == SameSiteMode.None,
            SameSite = sameSite,
            Path = path,
            Domain = siteWide && !string.IsNullOrWhiteSpace(config["Auth:SiteDomain"]) ? config["Auth:SiteDomain"] : null,
            Expires = expires,
            IsEssential = true,
        };
    }
}
