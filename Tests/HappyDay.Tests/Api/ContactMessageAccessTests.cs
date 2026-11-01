using System.Net;
using HappyDay.Tests.Infrastructure;

namespace HappyDay.Tests.Api;

/// <summary>GET api/contact-messages: hangi firmanın mesajlarının döneceğine handler oturumdan karar verir</summary>
[Collection(ApiCollection.Name)]
public class ContactMessageAccessTests(ApiFactory factory)
{
    private async Task<string> SendMessageAsync(Guid venueId)
    {
        var email = TestData.Email("talep");
        var response = await factory.Client().PostAsync("/api/contact-messages", Http.Form(
            ("OrganizationId", venueId.ToString()), ("FullName", "Ziyaretçi"), ("Phone", "05550000000"),
            ("Email", email), ("Message", "Teklif istiyorum")));
        Assert.True((await response.ResultAsync()).Ok);
        return email;
    }

    private static async Task<List<string>> EmailsAsync(HttpResponseMessage response)
    {
        var (ok, message, data) = await response.ResultAsync();
        Assert.True(ok, message);
        return data.EnumerateArray().Select(m => m.GetProperty("email").GetString()!).ToList();
    }

    [Fact]
    public async Task Company_sees_only_its_own_messages_even_when_it_asks_for_another_company()
    {
        var company = await factory.CreateCompanyAsync();
        var other = await factory.CreateCompanyAsync();
        var own = await SendMessageAsync((await factory.CreateVenueAsync(company.Id)).Id);
        var foreign = await SendMessageAsync((await factory.CreateVenueAsync(other.Id)).Id);
        var browser = await factory.LoggedInCompanyAsync(company);

        var emails = await EmailsAsync(await browser.GetAsync($"/api/contact-messages?companyId={other.Id}"));

        Assert.Contains(own, emails);
        Assert.DoesNotContain(foreign, emails);
    }

    [Fact]
    public async Task Admin_must_choose_the_company()
    {
        var company = await factory.CreateCompanyAsync();
        var message = await SendMessageAsync((await factory.CreateVenueAsync(company.Id)).Id);
        var admin = await factory.LoggedInAdminAsync();

        var withoutCompany = await (await admin.GetAsync("/api/contact-messages")).ResultAsync();
        var emails = await EmailsAsync(await admin.GetAsync($"/api/contact-messages?companyId={company.Id}"));

        Assert.False(withoutCompany.Ok);
        Assert.Equal("Admin için companyId zorunludur.", withoutCompany.Message);
        Assert.Equal([message], emails);
    }

    [Fact]
    public async Task Company_without_messages_gets_an_empty_list()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        Assert.Empty(await EmailsAsync(await browser.GetAsync("/api/contact-messages")));
    }

    [Fact]
    public async Task Users_without_a_company_cannot_list_messages()
    {
        var user = await factory.CreateUserAsync();
        var browser = factory.Browser();
        await browser.LoginUserAsync(user.Email, TestData.Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/contact-messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client().GetAsync("/api/contact-messages")).StatusCode);
    }
}
