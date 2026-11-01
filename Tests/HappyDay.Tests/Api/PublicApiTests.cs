using System.Net;
using HappyDay.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Tests.Api;

[Collection(ApiCollection.Name)]
public class PublicApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Migrations_load_provinces_by_plate_code()
    {
        var response = await factory.Client().GetAsync("/api/cities");
        var (ok, _, data) = await response.ResultAsync();

        Assert.True(ok);
        Assert.Equal(81, data.GetArrayLength());
        Assert.Contains(data.EnumerateArray(), c => c.GetProperty("id").GetInt32() == 34 && c.GetProperty("cityName").GetString() == "İstanbul");
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var response = await factory.Client().GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task Contact_message_with_honeypot_field_is_accepted_but_not_saved()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        var botEmail = TestData.Email("bot");
        var visitorEmail = TestData.Email("ziyaretci");

        var bot = await factory.Client().PostAsync("/api/contact-messages", Http.Form(
            ("OrganizationId", venue.Id.ToString()), ("FullName", "Bot"), ("Phone", "05550000000"),
            ("Email", botEmail), ("Message", "spam"), ("Website", "https://spam.test")));
        var visitor = await factory.Client().PostAsync("/api/contact-messages", Http.Form(
            ("OrganizationId", venue.Id.ToString()), ("FullName", "Ziyaretçi"), ("Phone", "05550000000"),
            ("Email", visitorEmail), ("Message", "Merhaba"), ("Website", "")));

        Assert.True((await bot.ResultAsync()).Ok);
        Assert.Equal("Mesajınız kaydedildi ve işletmeye iletildi.", (await visitor.ResultAsync()).Message);
        // Yalnızca gerçek ziyaretçinin talebi firmaya e-postayla gider
        var notice = Assert.Single(factory.Emails.To(company.Email));
        Assert.Equal("Yeni teklif talebi: Test Mekanı", notice.Subject);
        Assert.Contains(visitorEmail, notice.Body);
        Assert.Contains("https://frontend.test/panel/mesajlar", notice.Body);
        var saved = await factory.WithDbAsync(db => db.ContactMessages.Where(m => m.Email == botEmail || m.Email == visitorEmail).Select(m => m.Email).ToListAsync());
        Assert.Equal([visitorEmail], saved);
    }
}
