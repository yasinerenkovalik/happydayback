namespace HappyDay.Api.Middleware;

/// <summary>
/// Tüm yanıtlara (JSON ve /uploads altındaki dosyalar dahil) tarayıcı güvenlik başlıklarını ekler.
/// API HTML sunmadığı için CSP her şeyi kapatır; geliştirmedeki Swagger arayüzü bundan muaf.
/// </summary>
public class SecurityHeadersMiddleware
{
    private const string ApiCsp = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Başlıklar yanıt gönderilirken eklenir: hata yakalayıcı yanıtı temizlese de 500/409 yanıtlarında kalır
        context.Response.OnStarting(() =>
        {
            Apply(context);
            return Task.CompletedTask;
        });

        return _next(context);
    }

    private static void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Yüklenen dosya uzantısından farklı bir türde (ör. HTML) yorumlanmasın
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            headers.ContentSecurityPolicy = ApiCsp;
        }
    }
}
