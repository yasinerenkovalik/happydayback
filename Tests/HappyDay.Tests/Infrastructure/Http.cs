using System.Net.Http.Json;
using System.Text.Json;

namespace HappyDay.Tests.Infrastructure;

/// <summary>API yanıtları ({ data, isSuccess, message, errors }) ve çerezler için kısa yollar</summary>
public static class Http
{
    public const string ClientHeader = "X-Davetly-Client";
    public const string AccessCookie = "davetly.at";
    public const string RefreshCookie = "davetly.rt";
    public const string HintCookie = "davetly.session";

    public static Task<HttpResponseMessage> LoginCompanyAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/companies/login", new { email, password });

    public static Task<HttpResponseMessage> LoginPlatformAsync(this HttpClient client, string email, string password) =>
        client.PostAsync("/api/auth/login", Form(("Email", email), ("Password", password)));

    public static Task<HttpResponseMessage> LoginUserAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/users/login", new { email, password });

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>GeneralResponse'un isSuccess ve message alanları</summary>
    public static async Task<(bool Ok, string? Message, JsonElement Data)> ResultAsync(this HttpResponseMessage response)
    {
        var json = await response.JsonAsync();
        return (json.GetProperty("isSuccess").GetBoolean(),
                json.TryGetProperty("message", out var m) ? m.GetString() : null,
                json.TryGetProperty("data", out var d) ? d : default);
    }

    /// <summary>Yanıttaki Set-Cookie satırı (öznitelikleriyle), yoksa null</summary>
    public static string? SetCookie(this HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(name + "=", StringComparison.Ordinal))
            : null;

    public static string? CookieValue(this HttpResponseMessage response, string name) =>
        response.SetCookie(name)?.Split(';')[0][(name.Length + 1)..];

    /// <summary>Çerezi elle göndererek istek (Client() ile)</summary>
    public static Task<HttpResponseMessage> SendWithCookieAsync(this HttpClient client, HttpMethod method, string url, string cookie)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetWithBearerAsync(this HttpClient client, string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        return client.SendAsync(request);
    }

    public static MultipartFormDataContent Form(params (string Name, string Value)[] fields)
    {
        var form = new MultipartFormDataContent();
        foreach (var (name, value) in fields) form.Add(new StringContent(value), name);
        return form;
    }

    public static MultipartFormDataContent WithFile(this MultipartFormDataContent form, string name, string fileName, byte[] content, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new(contentType);
        form.Add(file, name, fileName);
        return form;
    }
}
