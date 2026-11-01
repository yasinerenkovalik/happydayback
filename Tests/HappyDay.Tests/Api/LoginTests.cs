using System.Net.Http.Json;
using HappyDay.Tests.Infrastructure;

namespace HappyDay.Tests.Api;

[Collection(ApiCollection.Name)]
public class LoginTests(ApiFactory factory)
{
    [Fact]
    public async Task Company_login_writes_httponly_session_cookies()
    {
        var company = await factory.CreateCompanyAsync();

        var response = await factory.Client().LoginCompanyAsync(company.Email, TestData.Password);

        Assert.True((await response.ResultAsync()).Ok);
        var access = response.SetCookie(Http.AccessCookie)?.ToLowerInvariant();
        var refresh = response.SetCookie(Http.RefreshCookie)?.ToLowerInvariant();
        var hint = response.SetCookie(Http.HintCookie)?.ToLowerInvariant();
        Assert.NotNull(access);
        Assert.Contains("httponly", access);
        Assert.Contains("samesite=strict", access);
        Assert.Contains("secure", access);
        Assert.Contains("path=/;", access + ";");
        Assert.NotNull(refresh);
        Assert.Contains("httponly", refresh);
        Assert.Contains("path=/api/auth", refresh); // refresh token başka adrese gitmez
        Assert.NotNull(hint);
        Assert.DoesNotContain("httponly", hint);    // frontend okuyabilsin; sır içermez
    }

    [Fact]
    public async Task Session_endpoint_returns_the_logged_in_company()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        var (ok, _, data) = await (await browser.GetAsync("/api/auth/session")).ResultAsync();

        Assert.True(ok);
        Assert.Equal("Company", data.GetProperty("role").GetString());
        Assert.Equal(company.Id, data.GetProperty("companyId").GetGuid());
    }

    [Fact]
    public async Task Email_is_matched_ignoring_case()
    {
        var company = await factory.CreateCompanyAsync();

        var response = await factory.Client().LoginCompanyAsync(company.Email.ToUpperInvariant(), TestData.Password);

        Assert.True((await response.ResultAsync()).Ok);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        var company = await factory.CreateCompanyAsync();
        var client = factory.Client();

        for (var i = 1; i <= 4; i++)
        {
            var (ok, message, _) = await (await client.LoginCompanyAsync(company.Email, "yanlis-sifre1")).ResultAsync();
            Assert.False(ok);
            Assert.Equal("E-posta veya şifre hatalı.", message);
        }
        var fifth = await (await client.LoginCompanyAsync(company.Email, "yanlis-sifre1")).ResultAsync();
        var correct = await (await client.LoginCompanyAsync(company.Email, TestData.Password)).ResultAsync();

        Assert.Contains("dakika sonra tekrar deneyin", fifth.Message);
        Assert.False(correct.Ok);
        Assert.Contains("dakika sonra tekrar deneyin", correct.Message);
    }

    [Fact]
    public async Task Only_platform_login_grants_the_admin_role()
    {
        var admin = await factory.CreateAdminAsync();

        var viaUsers = factory.Browser();
        await viaUsers.LoginUserAsync(admin.Email, TestData.Password);
        var viaPlatform = factory.Browser();
        await viaPlatform.LoginPlatformAsync(admin.Email, TestData.Password);

        Assert.Equal("User", (await (await viaUsers.GetAsync("/api/auth/session")).ResultAsync()).Data.GetProperty("role").GetString());
        Assert.Equal("Admin", (await (await viaPlatform.GetAsync("/api/auth/session")).ResultAsync()).Data.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Registration_rejects_weak_passwords_and_duplicate_emails()
    {
        var email = TestData.Email("kayit");
        static MultipartFormDataContent Registration(string email, string password) => Http.Form(
            ("Name", "Yeni Firma"), ("Email", email), ("Password", password), ("PhoneNumber", "0555 111 22 33"),
            ("Description", "Açıklama"), ("Adress", "Adres"), ("CityId", "34"), ("DistrictId", "34001"),
            ("Latitude", "41.0"), ("Longitude", "29.0"));
        var client = factory.Client();

        var weak = await client.PostAsync("/api/companies", Registration(email, "kisa"));
        var created = await client.PostAsync("/api/companies", Registration(email, TestData.Password));
        var duplicate = await client.PostAsync("/api/companies", Registration(email.ToUpperInvariant(), TestData.Password));

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.True((await created.ResultAsync()).Ok);
        var (dupOk, dupMessage, _) = await duplicate.ResultAsync();
        Assert.False(dupOk);
        Assert.Contains("zaten", dupMessage);
    }

    [Fact]
    public async Task Concurrent_registrations_with_one_email_create_a_single_account()
    {
        var email = TestData.Email("yaris");
        var requests = Enumerable.Range(0, 4).Select(_ => factory.Client().PostAsJsonAsync("/api/users", new
        {
            name = "Yarış", surName = "Test", email, password = TestData.Password,
            birtDay = "1990-01-01T00:00:00Z", adress = "A", phoneNumber = "05550000000", sex = true,
        }));

        var responses = await Task.WhenAll(requests);

        // Ön kontrolü birlikte geçen istekler tekil indekse takılır: 500 değil 409 dönmeli
        Assert.All(responses, r => Assert.Contains((int)r.StatusCode, new[] { 200, 409 }));
        var count = await factory.WithDbAsync(db => Task.FromResult(db.Users.Count(u => u.Email == email && u.IsActivated)));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Company_must_confirm_its_email_before_logging_in()
    {
        var email = TestData.Email("onaysiz");
        await factory.Client().PostAsync("/api/companies", Http.Form(
            ("Name", "Onaysız Firma"), ("Email", email), ("Password", TestData.Password), ("PhoneNumber", "05551112233"),
            ("Description", "Açıklama"), ("Adress", "Adres"), ("CityId", "34"), ("DistrictId", "34001"),
            ("Latitude", "41.0"), ("Longitude", "29.0")));
        var client = factory.Client();

        var wrongPassword = await (await client.LoginCompanyAsync(email, "yanlis-sifre1")).ResultAsync();
        var beforeConfirm = await client.LoginCompanyAsync(email, TestData.Password);
        var beforeConfirmPlatform = await client.LoginPlatformAsync(email, TestData.Password);

        // Şifreyi bilmeyen biri hesabın doğrulanıp doğrulanmadığını öğrenemez
        Assert.Equal("E-posta veya şifre hatalı.", wrongPassword.Message);
        var json = await beforeConfirm.JsonAsync();
        Assert.False(json.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("EMAIL_NOT_CONFIRMED", json.GetProperty("errors")[0].GetString());
        Assert.Null(beforeConfirm.SetCookie(Http.AccessCookie));
        Assert.False((await beforeConfirmPlatform.ResultAsync()).Ok);

        var (companyId, token) = factory.Emails.LastLink(email, "confirm-email");
        await client.PostAsJsonAsync("/api/companies/confirm-email", new { companyId, token });

        Assert.True((await (await client.LoginCompanyAsync(email, TestData.Password)).ResultAsync()).Ok);
    }

    [Fact]
    public async Task Verification_link_confirms_the_email_once()
    {
        var email = TestData.Email("dogrulama");
        await factory.Client().PostAsync("/api/companies", Http.Form(
            ("Name", "Doğrulama Firması"), ("Email", email), ("Password", TestData.Password), ("PhoneNumber", "05551112233"),
            ("Description", "Açıklama"), ("Adress", "Adres"), ("CityId", "34"), ("DistrictId", "34001"),
            ("Latitude", "41.0"), ("Longitude", "29.0")));
        var (companyId, token) = factory.Emails.LastLink(email, "confirm-email");
        var client = factory.Client();

        var first = await client.PostAsJsonAsync("/api/companies/confirm-email", new { companyId, token });
        var second = await client.PostAsJsonAsync("/api/companies/confirm-email", new { companyId, token });

        Assert.True((await first.ResultAsync()).Ok);
        Assert.False((await second.ResultAsync()).Ok);
    }
}
