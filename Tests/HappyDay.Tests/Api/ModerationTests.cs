using System.Net;
using System.Net.Http.Json;
using HappyDay.Tests.Infrastructure;

namespace HappyDay.Tests.Api;

/// <summary>Admin uygunsuz mekanı yayından kaldırır: sitede görünmez, silinmez, firma sebebi görür</summary>
[Collection(ApiCollection.Name)]
public class ModerationTests(ApiFactory factory)
{
    private const string Reason = "Galerideki fotoğraflar başka bir mekana ait.";

    private Task<bool> IsPublicAsync(Guid venueId) =>
        factory.Client().GetAsync($"/api/organizations/{venueId}/with-images").ContinueWith(t => t.Result.ResultAsync().Result.Ok);

    private async Task<bool> InPublicListsAsync(Guid venueId)
    {
        var filter = await (await factory.Client().GetAsync("/api/organizations/filter?pageNumber=1&pageSize=200")).ResultAsync();
        var all = await (await factory.Client().GetAsync("/api/organizations?pageNumber=1&pageSize=200")).ResultAsync();
        var inFilter = filter.Data.GetProperty("items").EnumerateArray().Any(v => v.GetProperty("id").GetGuid() == venueId);
        var inAll = all.Ok && all.Data.GetProperty("items").EnumerateArray().Any(v => v.GetProperty("id").GetGuid() == venueId);
        Assert.Equal(inFilter, inAll); // iki liste aynı kuralı uygular
        return inFilter;
    }

    [Fact]
    public async Task Removed_venue_disappears_from_the_site_and_the_company_is_told_why()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        var admin = await factory.LoggedInAdminAsync();
        Assert.True(await IsPublicAsync(venue.Id));

        var remove = await admin.PostAsJsonAsync($"/api/admin/organizations/{venue.Id}/remove", new { reason = Reason });

        Assert.True((await remove.ResultAsync()).Ok);
        Assert.False(await IsPublicAsync(venue.Id));
        Assert.False(await InPublicListsAsync(venue.Id));
        Assert.False((await (await factory.Client().GetAsync($"/api/organizations/{venue.Id}")).ResultAsync()).Ok);

        // Teklif gönderilemez
        var inquiry = await factory.Client().PostAsync("/api/contact-messages", Http.Form(
            ("OrganizationId", venue.Id.ToString()), ("FullName", "Ziyaretçi"), ("Phone", "05550000000"),
            ("Email", TestData.Email("ziyaretci")), ("Message", "Merhaba")));
        Assert.False((await inquiry.ResultAsync()).Ok);

        // Firma silinmemiş mekanını ve sebebini panelde görür, e-postayla da öğrenir
        var owner = await factory.LoggedInCompanyAsync(company);
        var managed = await (await owner.GetAsync($"/api/organizations/{venue.Id}/manage")).ResultAsync();
        Assert.True(managed.Ok);
        Assert.Equal(Reason, managed.Data.GetProperty("removalReason").GetString());
        var ownList = await (await owner.GetAsync($"/api/organizations/by-company/{company.Id}")).ResultAsync();
        Assert.Contains(ownList.Data.EnumerateArray(), v => v.GetProperty("removalReason").GetString() == Reason);
        var mail = factory.Emails.To(company.Email).Last();
        Assert.Equal("Mekanınız yayından kaldırıldı: Test Mekanı", mail.Subject);
        Assert.Contains(Reason, mail.Body);
    }

    [Fact]
    public async Task Restored_venue_is_public_again()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        var admin = await factory.LoggedInAdminAsync();
        await admin.PostAsJsonAsync($"/api/admin/organizations/{venue.Id}/remove", new { reason = Reason });

        var restore = await admin.PostAsync($"/api/admin/organizations/{venue.Id}/restore", null);

        Assert.True((await restore.ResultAsync()).Ok);
        Assert.True(await IsPublicAsync(venue.Id));
        Assert.True(await InPublicListsAsync(venue.Id));
        Assert.Equal("Mekanınız yeniden yayında: Test Mekanı", factory.Emails.To(company.Email).Last().Subject);
    }

    [Fact]
    public async Task Removal_needs_a_reason_and_only_admins_can_remove()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        var admin = await factory.LoggedInAdminAsync();
        var owner = await factory.LoggedInCompanyAsync(company);

        var noReason = await admin.PostAsJsonAsync($"/api/admin/organizations/{venue.Id}/remove", new { reason = "kötü" });
        var byCompany = await owner.PostAsJsonAsync($"/api/admin/organizations/{venue.Id}/remove", new { reason = Reason });
        var anonymous = await factory.Client().PostAsJsonAsync($"/api/admin/organizations/{venue.Id}/remove", new { reason = Reason });

        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byCompany.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.True(await IsPublicAsync(venue.Id));
    }

    [Fact]
    public async Task Admin_list_filters_by_status()
    {
        var approved = await factory.CreateCompanyAsync();
        var unapproved = await factory.CreateCompanyAsync(approved: false);
        var published = await factory.CreateVenueAsync(approved.Id);
        var removed = await factory.CreateVenueAsync(approved.Id);
        var waiting = await factory.CreateVenueAsync(unapproved.Id);
        var admin = await factory.LoggedInAdminAsync();
        await admin.PostAsJsonAsync($"/api/admin/organizations/{removed.Id}/remove", new { reason = Reason });

        async Task<List<Guid>> IdsAsync(string status) =>
            (await (await admin.GetAsync($"/api/admin/organizations?status={status}&pageSize=200")).ResultAsync())
                .Data.EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).ToList();

        var publishedIds = await IdsAsync("Published");
        var removedIds = await IdsAsync("Removed");
        var unapprovedIds = await IdsAsync("Unapproved");

        Assert.Contains(published.Id, publishedIds);
        Assert.DoesNotContain(removed.Id, publishedIds);
        Assert.DoesNotContain(waiting.Id, publishedIds);
        Assert.Contains(removed.Id, removedIds);
        Assert.DoesNotContain(published.Id, removedIds);
        Assert.Contains(waiting.Id, unapprovedIds);
    }

    [Fact]
    public async Task Visitors_see_only_published_venues_of_a_company()
    {
        var company = await factory.CreateCompanyAsync(approved: false);
        var venue = await factory.CreateVenueAsync(company.Id);
        var owner = await factory.LoggedInCompanyAsync(company);

        var asVisitor = await (await factory.Client().GetAsync($"/api/organizations/by-company/{company.Id}")).ResultAsync();
        var asOwner = await (await owner.GetAsync($"/api/organizations/by-company/{company.Id}")).ResultAsync();

        Assert.DoesNotContain(asVisitor.Data.EnumerateArray(), v => v.GetProperty("id").GetGuid() == venue.Id);
        Assert.Contains(asOwner.Data.EnumerateArray(), v => v.GetProperty("id").GetGuid() == venue.Id);
    }
}
