using System.Net;
using System.Net.Http.Json;
using HappyDay.Application.Common.Security;
using HappyDay.Persistance.Security;
using HappyDay.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HappyDay.Tests.Api;

/// <summary>Oturum kapanınca access token süresi dolmadan geçersiz olmalı</summary>
[Collection(ApiCollection.Name)]
public class SessionTests(ApiFactory factory)
{
    private async Task<(string Access, string Refresh)> LoginAsync(string email)
    {
        var response = await factory.Client().LoginCompanyAsync(email, TestData.Password);
        Assert.True((await response.ResultAsync()).Ok);
        return (response.CookieValue(Http.AccessCookie)!, response.CookieValue(Http.RefreshCookie)!);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken, bool clientHeader = true) =>
        factory.Client(clientHeader).SendWithCookieAsync(HttpMethod.Post, "/api/auth/refresh", $"{Http.RefreshCookie}={refreshToken}");

    private Task<HttpStatusCode> SessionStatusAsync(string accessToken) =>
        factory.Client().GetWithBearerAsync("/api/auth/session", accessToken).ContinueWith(t => t.Result.StatusCode);

    [Fact]
    public async Task Refresh_issues_new_tokens_and_rotates_the_refresh_token()
    {
        var company = await factory.CreateCompanyAsync();
        var (_, refresh) = await LoginAsync(company.Email);

        var response = await RefreshAsync(refresh);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = response.CookieValue(Http.RefreshCookie);
        Assert.NotNull(rotated);
        Assert.NotEqual(refresh, rotated);
        Assert.Equal(HttpStatusCode.OK, await SessionStatusAsync(response.CookieValue(Http.AccessCookie)!));
    }

    [Fact]
    public async Task Refresh_requires_the_client_header()
    {
        var company = await factory.CreateCompanyAsync();
        var (_, refresh) = await LoginAsync(company.Email);

        var response = await RefreshAsync(refresh, clientHeader: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reusing_an_old_refresh_token_closes_the_session()
    {
        var company = await factory.CreateCompanyAsync();
        var (_, first) = await LoginAsync(company.Email);
        var second = (await RefreshAsync(first)).CookieValue(Http.RefreshCookie)!;
        var thirdResponse = await RefreshAsync(second);
        var third = thirdResponse.CookieValue(Http.RefreshCookie)!;
        var access = thirdResponse.CookieValue(Http.AccessCookie)!;

        // İlk token iki dönüş geride: ne güncel ne bir önceki → çalınmış sayılır
        var reuse = await RefreshAsync(first);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(third)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(access));
    }

    [Fact]
    public async Task Logout_invalidates_the_access_token_immediately()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = factory.Browser();
        var login = await browser.LoginCompanyAsync(company.Email, TestData.Password);
        var copiedToken = login.CookieValue(Http.AccessCookie)!;

        var logout = await browser.PostAsync("/api/auth/logout", null);

        Assert.True((await logout.ResultAsync()).Ok);
        Assert.Contains("expires=thu, 01 jan 1970", logout.SetCookie(Http.AccessCookie)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(copiedToken));
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Signed_token_without_an_open_session_is_rejected()
    {
        var company = await factory.CreateCompanyAsync();
        var (forged, _) = factory.GetService(sp => sp.GetRequiredService<JwtService>())
            .CreateAccessToken(AccountPrincipal.ForCompany(company), sessionId: Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(forged));
    }

    [Fact]
    public async Task Validate_token_endpoint_rejects_tokens_of_closed_sessions()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = factory.Browser();
        var token = (await browser.LoginCompanyAsync(company.Email, TestData.Password)).CookieValue(Http.AccessCookie);
        var url = $"/api/users/validate-token?token={token}";

        var before = await factory.Client().GetAsync(url);
        await browser.PostAsync("/api/auth/logout", null);
        var after = await factory.Client().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        var json = await before.JsonAsync();
        Assert.True(json.GetProperty("isValid").GetBoolean());
        Assert.Equal("Company", json.GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client().GetAsync("/api/users/validate-token?token=bozuk")).StatusCode);
    }

    [Fact]
    public async Task Cookie_requests_that_change_data_need_the_client_header()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        var (access, _) = await LoginAsync(company.Email);

        var withoutHeader = await factory.Client(clientHeader: false)
            .SendWithCookieAsync(HttpMethod.Delete, $"/api/organizations/{venue.Id}", $"{Http.AccessCookie}={access}");
        var readWithoutHeader = await factory.Client(clientHeader: false)
            .SendWithCookieAsync(HttpMethod.Get, "/api/auth/session", $"{Http.AccessCookie}={access}");

        Assert.Equal(HttpStatusCode.Unauthorized, withoutHeader.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readWithoutHeader.StatusCode);
        Assert.True(await factory.WithDbAsync(db => Task.FromResult(db.Organizations.Single(o => o.Id == venue.Id).IsActivated)));
    }

    [Fact]
    public async Task Changing_the_password_closes_other_sessions_but_keeps_this_one()
    {
        var company = await factory.CreateCompanyAsync();
        var (otherDevice, _) = await LoginAsync(company.Email);
        var browser = await factory.LoggedInCompanyAsync(company);

        var wrong = await browser.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "yanlis-sifre1", newPassword = "YeniSifre99" });
        var changed = await browser.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = TestData.Password, newPassword = "YeniSifre99" });

        Assert.Equal("Mevcut şifre hatalı.", (await wrong.ResultAsync()).Message);
        Assert.True((await changed.ResultAsync()).Ok);
        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(otherDevice));
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/session")).StatusCode);
        Assert.False((await (await factory.Client().LoginCompanyAsync(company.Email, TestData.Password)).ResultAsync()).Ok);
        Assert.True((await (await factory.Client().LoginCompanyAsync(company.Email, "YeniSifre99")).ResultAsync()).Ok);
    }

    [Fact]
    public async Task Password_reset_closes_every_session()
    {
        var company = await factory.CreateCompanyAsync();
        var (session, _) = await LoginAsync(company.Email);

        await factory.Client().PostAsJsonAsync("/api/companies/request-password-reset", new { email = company.Email });
        var (companyId, token) = factory.Emails.LastLink(company.Email, "reset-password");
        var reset = await factory.Client().PostAsJsonAsync("/api/companies/reset-password", new { companyId, token, newPassword = "Sifirlandi99" });

        Assert.True((await reset.ResultAsync()).Ok);
        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(session));
        Assert.True((await (await factory.Client().LoginCompanyAsync(company.Email, "Sifirlandi99")).ResultAsync()).Ok);
    }

    [Fact]
    public async Task Deactivated_user_loses_the_session()
    {
        var user = await factory.CreateUserAsync();
        var login = await factory.Client().LoginUserAsync(user.Email, TestData.Password);
        var userToken = login.CookieValue(Http.AccessCookie)!;
        var admin = await factory.LoggedInAdminAsync();

        var deactivate = await admin.PostAsync("/api/admin/users/active", Http.Form(("UserId", user.Id.ToString()), ("IsActivated", "false")));

        Assert.True((await deactivate.ResultAsync()).Ok);
        Assert.Equal(HttpStatusCode.Unauthorized, await SessionStatusAsync(userToken));
    }
}
