using HappyDay.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Tests.Api;

/// <summary>Mekanın uygun olduğu etkinlikler: firma seçer, site bunlara göre filtreler</summary>
[Collection(ApiCollection.Name)]
public class EventTypeTests(ApiFactory factory)
{
    private const int Dugun = 1, Nisan = 2, DogumGunu = 7;

    private MultipartFormDataContent NewVenue(Guid companyId, params int[] eventTypeIds) => Http.Form(
        [
            ("Title", "Etkinlik Mekanı"), ("Description", "Açıklama"), ("Price", "1000"), ("MaxGuestCount", "100"),
            ("CategoryId", factory.CategoryId.ToString()), ("Duration", "5 saat"), ("IsOutdoor", "true"),
            ("CompanyId", companyId.ToString()),
            .. eventTypeIds.Select(id => ("EventTypeIds", id.ToString())),
        ]);

    private async Task<List<Guid>> FilterAsync(string eventType)
    {
        var result = await (await factory.Client().GetAsync($"/api/organizations/filter?eventType={eventType}&pageSize=100")).ResultAsync();
        Assert.True(result.Ok);
        return result.Data.GetProperty("items").EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).ToList();
    }

    [Fact]
    public async Task Event_types_are_listed_in_site_order()
    {
        var result = await (await factory.Client().GetAsync("/api/event-types")).ResultAsync();

        var items = result.Data.EnumerateArray().ToList();
        Assert.Equal(11, items.Count);
        Assert.Equal((1, "Düğün", "dugun"), (items[0].GetProperty("id").GetInt32(), items[0].GetProperty("name").GetString(), items[0].GetProperty("slug").GetString()));
        Assert.Contains(items, e => e.GetProperty("slug").GetString() == "dogum-gunu");
    }

    [Fact]
    public async Task Company_chooses_event_types_and_the_site_filters_by_them()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        var created = await (await browser.PostAsync("/api/organizations", NewVenue(company.Id, Nisan, Dugun, Nisan))).ResultAsync();

        Assert.True(created.Ok, created.Message);
        var id = created.Data.GetProperty("id").GetGuid();
        var detail = await (await factory.Client().GetAsync($"/api/organizations/{id}/with-images")).ResultAsync();
        Assert.Equal([Dugun, Nisan], detail.Data.GetProperty("eventTypeIds").EnumerateArray().Select(e => e.GetInt32())); // tekrarsız, sıralı
        Assert.Contains(id, await FilterAsync("nisan"));
        Assert.Contains(id, await FilterAsync("DUGUN"));
        Assert.DoesNotContain(id, await FilterAsync("dogum-gunu"));
        Assert.Empty(await FilterAsync("boyle-bir-etkinlik-yok"));
    }

    [Fact]
    public async Task New_venue_needs_at_least_one_known_event_type()
    {
        var company = await factory.CreateCompanyAsync();
        var browser = await factory.LoggedInCompanyAsync(company);

        var none = await (await browser.PostAsync("/api/organizations", NewVenue(company.Id))).ResultAsync();
        var unknown = await (await browser.PostAsync("/api/organizations", NewVenue(company.Id, Dugun, 99))).ResultAsync();

        Assert.False(none.Ok);
        Assert.Contains("en az bir etkinlik", none.Message);
        Assert.False(unknown.Ok);
        Assert.Equal("Geçersiz etkinlik türü.", unknown.Message);
        Assert.False(await factory.WithDbAsync(db => db.Organizations.AnyAsync(o => o.CompanyId == company.Id)));
    }

    [Fact]
    public async Task Update_keeps_event_types_unless_new_ones_are_sent()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        await factory.WithDbAsync(async db =>
        {
            (await db.Organizations.SingleAsync(o => o.Id == venue.Id)).EventTypeIds = [Dugun];
            return await db.SaveChangesAsync();
        });
        var browser = await factory.LoggedInCompanyAsync(company);
        Task<List<int>> Saved() => factory.WithDbAsync(db => db.Organizations.Where(o => o.Id == venue.Id).Select(o => o.EventTypeIds).SingleAsync());

        // Etkinlik gönderilmedi: seçim korunur
        Assert.True((await (await browser.PutAsync($"/api/organizations/{venue.Id}", Http.Form(("Title", "Yeni Ad"), ("Price", "1"), ("MaxGuestCount", "10")))).ResultAsync()).Ok);
        Assert.Equal([Dugun], await Saved());

        Assert.True((await (await browser.PutAsync($"/api/organizations/{venue.Id}", Http.Form(("Price", "1"), ("MaxGuestCount", "10"), ("EventTypeIds", DogumGunu.ToString())))).ResultAsync()).Ok);
        Assert.Equal([DogumGunu], await Saved());

        var invalid = await (await browser.PutAsync($"/api/organizations/{venue.Id}", Http.Form(("Price", "1"), ("MaxGuestCount", "10"), ("EventTypeIds", "0")))).ResultAsync();
        Assert.False(invalid.Ok);
        Assert.Equal([DogumGunu], await Saved());
    }
}
