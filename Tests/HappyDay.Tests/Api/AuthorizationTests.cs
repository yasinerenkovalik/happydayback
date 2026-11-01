using System.Net;
using HappyDay.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Tests.Api;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(ApiFactory factory)
{
    private const string Forbidden = "Bu işlem için yetkiniz yok.";

    private static MultipartFormDataContent VenueUpdate(string title) => Http.Form(
        ("Title", title), ("Price", "1"), ("MaxGuestCount", "10"), ("IsOutdoor", "false"), ("Services", "DJ"));

    [Fact]
    public async Task Admin_endpoints_reject_anonymous_users_and_companies()
    {
        var company = await factory.CreateCompanyAsync();
        var companyBrowser = await factory.LoggedInCompanyAsync(company);
        var admin = await factory.LoggedInAdminAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client().GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await companyBrowser.GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Company_cannot_touch_another_companys_venue()
    {
        var owner = await factory.CreateCompanyAsync();
        var other = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(owner.Id);
        var intruder = await factory.LoggedInCompanyAsync(other);

        var update = await intruder.PutAsync($"/api/organizations/{venue.Id}", VenueUpdate("Ele geçirildi"));
        var delete = await intruder.DeleteAsync($"/api/organizations/{venue.Id}");
        var manage = await intruder.GetAsync($"/api/organizations/{venue.Id}/manage");
        var image = await intruder.PostAsync("/api/organization-images", Http.Form(("OrganizationId", venue.Id.ToString()))
            .WithFile("OrganizationImage", "foto.png", TestData.Images.Png, "image/png"));

        Assert.Equal(Forbidden, (await update.ResultAsync()).Message);
        Assert.Equal(Forbidden, (await delete.ResultAsync()).Message);
        Assert.Equal(Forbidden, (await manage.ResultAsync()).Message);
        Assert.Equal(Forbidden, (await image.ResultAsync()).Message);
        var stored = await factory.WithDbAsync(db => db.Organizations.Include(o => o.OrganizationImages).SingleAsync(o => o.Id == venue.Id));
        Assert.Equal("Test Mekanı", stored.Title);
        Assert.True(stored.IsActivated);
        Assert.Empty(stored.OrganizationImages);
    }

    [Fact]
    public async Task Company_can_manage_its_own_venue()
    {
        var owner = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(owner.Id);
        var browser = await factory.LoggedInCompanyAsync(owner);

        var update = await browser.PutAsync($"/api/organizations/{venue.Id}", VenueUpdate("Yeni Ad"));

        Assert.True((await update.ResultAsync()).Ok);
        Assert.Equal("Yeni Ad", await factory.WithDbAsync(db => db.Organizations.Where(o => o.Id == venue.Id).Select(o => o.Title).SingleAsync()));
    }

    [Fact]
    public async Task Company_cannot_create_a_venue_for_another_company()
    {
        var company = await factory.CreateCompanyAsync();
        var victim = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        var response = await browser.PostAsync("/api/organizations", Http.Form(
            ("Title", "Sahte"), ("Description", "x"), ("Price", "1"), ("MaxGuestCount", "10"), ("CategoryId", factory.CategoryId.ToString()),
            ("Duration", "1 saat"), ("IsOutdoor", "false"), ("CompanyId", victim.Id.ToString())));

        Assert.Equal(Forbidden, (await response.ResultAsync()).Message);
        Assert.False(await factory.WithDbAsync(db => db.Organizations.AnyAsync(o => o.CompanyId == victim.Id)));
    }

    [Fact]
    public async Task Unapproved_company_venue_is_hidden_publicly_but_visible_to_its_owner()
    {
        var company = await factory.CreateCompanyAsync(approved: false);
        var venue = await factory.CreateVenueAsync(company.Id);
        var owner = await factory.LoggedInCompanyAsync(company);

        var publicDetail = await factory.Client().GetAsync($"/api/organizations/{venue.Id}/with-images");
        var publicList = await factory.Client().GetAsync("/api/organizations/filter?pageNumber=1&pageSize=200");
        var managed = await owner.GetAsync($"/api/organizations/{venue.Id}/manage");

        Assert.False((await publicDetail.ResultAsync()).Ok);
        var items = (await publicList.ResultAsync()).Data.GetProperty("items").EnumerateArray();
        Assert.DoesNotContain(items, v => v.GetProperty("id").GetGuid() == venue.Id);
        Assert.True((await managed.ResultAsync()).Ok);
    }

    [Fact]
    public async Task Venue_district_must_belong_to_its_city()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        var response = await browser.PostAsync("/api/organizations", Http.Form(
            ("Title", "Yanlış İlçe"), ("Description", "x"), ("Price", "1"), ("MaxGuestCount", "10"), ("CategoryId", factory.CategoryId.ToString()),
            ("Duration", "1 saat"), ("IsOutdoor", "false"), ("CompanyId", company.Id.ToString()),
            ("CityId", TestData.Ankara.ToString()), ("DistrictId", TestData.IstanbulDistrict.ToString())));

        Assert.Equal("Seçilen ilçe bu ile ait değil.", (await response.ResultAsync()).Message);
    }
}
