using System.Net;
using System.Xml.Linq;
using HappyDay.Persistance.Context;
using HappyDay.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HappyDay.Tests.Api;

/// <summary>sitemap.xml, robots.txt ve paylaşım önizlemesi yalnızca yayındaki mekanları gösterir</summary>
[Collection(ApiCollection.Name)]
public class SeoTests(ApiFactory factory)
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private async Task<List<string>> SitemapUrlsAsync()
    {
        var response = await factory.Client().GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        return xml.Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value).ToList();
    }

    private async Task UpdateVenueAsync(Guid id, Action<HappyDay.Domain.Entities.Organization> change)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyDayContext>();
        change(await db.Organizations.FindAsync(id) ?? throw new InvalidOperationException());
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sitemap_lists_the_home_page_and_only_published_venues()
    {
        var published = await factory.CreateVenueAsync((await factory.CreateCompanyAsync()).Id);
        var unapproved = await factory.CreateVenueAsync((await factory.CreateCompanyAsync(approved: false)).Id);
        var removed = await factory.CreateVenueAsync((await factory.CreateCompanyAsync()).Id);
        await UpdateVenueAsync(removed.Id, v => v.RemovedByAdminAt = DateTime.UtcNow);

        var urls = await SitemapUrlsAsync();

        Assert.Contains("https://frontend.test/", urls); // Frontend:BaseUrl
        Assert.Contains($"https://frontend.test/mekan/{published.Id}", urls);
        Assert.DoesNotContain($"https://frontend.test/mekan/{unapproved.Id}", urls);
        Assert.DoesNotContain($"https://frontend.test/mekan/{removed.Id}", urls);
    }

    [Fact]
    public async Task Api_robots_txt_hides_the_api_but_not_images_or_the_sitemap()
    {
        var robots = await factory.Client().GetStringAsync("/robots.txt");

        Assert.Contains("Disallow: /\n", robots);
        Assert.Contains("Allow: /uploads/", robots);
        Assert.Contains("Allow: /sitemap.xml", robots);
    }

    [Fact]
    public async Task Preview_has_share_tags_with_encoded_text_and_an_absolute_https_image()
    {
        var venue = await factory.CreateVenueAsync((await factory.CreateCompanyAsync()).Id, coverPhotoPath: "uploads/cover/kapak.jpg");
        await UpdateVenueAsync(venue.Id, v =>
        {
            v.Title = "Gül & Bahçe <script>alert(1)</script>";
            v.Description = "Deniz kenarında \"özel\" bir bahçe.";
            v.EventTypeIds = [2, 1]; // nişan, düğün
        });

        // Traefik'in eklediği başlıklar: API önizlemedeki görsel adresini https ile kurmalı
        var request = new HttpRequestMessage(HttpMethod.Get, $"/seo/mekan/{venue.Id}");
        request.Headers.Host = "api.example.test";
        request.Headers.Add("X-Forwarded-Proto", "https");
        var response = await factory.Client().SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("<meta property=\"og:title\" content=\"Gül &amp; Bahçe &lt;script&gt;alert(1)&lt;/script&gt; — ", html);
        Assert.Contains(" Düğün Mekanı | Mutlu Günüm\">", html); // ilk etkinlik başlıkta
        Assert.Contains(" · Düğün, Nişan · ", html);
        Assert.Contains("&quot;özel&quot;", html);
        Assert.Contains($"<link rel=\"canonical\" href=\"https://frontend.test/mekan/{venue.Id}\">", html);
        Assert.Contains("<meta property=\"og:image\" content=\"https://api.example.test/uploads/cover/kapak.jpg\">", html);
        Assert.Contains("50.000 ₺", html); // fiyat Türkçe biçimde
    }

    [Fact]
    public async Task Preview_is_not_found_for_a_venue_that_is_not_published()
    {
        var unapproved = await factory.CreateVenueAsync((await factory.CreateCompanyAsync(approved: false)).Id);

        Assert.Equal(HttpStatusCode.NotFound, (await factory.Client().GetAsync($"/seo/mekan/{unapproved.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.Client().GetAsync($"/seo/mekan/{Guid.NewGuid()}")).StatusCode);
    }
}
