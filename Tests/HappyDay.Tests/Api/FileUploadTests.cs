using System.Net;
using HappyDay.Domain.Entities;
using HappyDay.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Tests.Api;

[Collection(ApiCollection.Name)]
public class FileUploadTests(ApiFactory factory)
{
    private string DiskPath(string webPath) => Path.Combine(factory.UploadRoot, webPath.TrimStart('/'));

    private async Task<(HttpClient Browser, Organization Venue)> OwnVenueAsync()
    {
        var company = await factory.CreateCompanyAsync();
        var venue = await factory.CreateVenueAsync(company.Id);
        return (await factory.LoggedInCompanyAsync(company), venue);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient browser, Guid venueId, string fileName, byte[] content, string contentType) =>
        browser.PostAsync("/api/organization-images", Http.Form(("OrganizationId", venueId.ToString()))
            .WithFile("OrganizationImage", fileName, content, contentType));

    public static TheoryData<string, byte[], string> RejectedFiles => new()
    {
        { "sahte.jpg", "<html><script>alert(1)</script></html>"u8.ToArray(), "uyuşmuyor" },
        { "png-ama-adi.jpg", TestData.Images.Png, "uyuşmuyor" },
        { "bos.png", [], "boş" },
        { "buyuk.png", [.. TestData.Images.Png, .. new byte[10 * 1024 * 1024]], "çok büyük" },
        { "cizim.svg", "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(), "Desteklenmeyen" },
    };

    [Theory]
    [MemberData(nameof(RejectedFiles))]
    public async Task Rejects_files_that_are_not_what_they_claim(string fileName, byte[] content, string expectedMessage)
    {
        var (browser, venue) = await OwnVenueAsync();

        var response = await UploadAsync(browser, venue.Id, fileName, content, "image/png");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedMessage, (await response.ResultAsync()).Message);
        Assert.False(await factory.WithDbAsync(db => db.OrganizationImages.AnyAsync(i => i.OrganizationId == venue.Id)));
    }

    [Theory]
    [InlineData("foto.png", "image/png")]
    [InlineData("foto.jpg", "image/jpeg")]
    [InlineData("hareketli.gif", "image/gif")]
    public async Task Stores_real_images_and_removes_the_file_when_the_image_is_deleted(string fileName, string contentType)
    {
        var (browser, venue) = await OwnVenueAsync();
        var content = contentType switch { "image/png" => TestData.Images.Png, "image/jpeg" => TestData.Images.Jpeg, _ => TestData.Images.Gif };

        var upload = await UploadAsync(browser, venue.Id, fileName, content, contentType);
        var image = await factory.WithDbAsync(db => db.OrganizationImages.SingleAsync(i => i.OrganizationId == venue.Id));

        Assert.True((await upload.ResultAsync()).Ok);
        Assert.StartsWith("/uploads/organization/", image.ImageUrl);
        Assert.True(File.Exists(DiskPath(image.ImageUrl)));

        var delete = await browser.DeleteAsync($"/api/organization-images/{image.Id}");

        Assert.True((await delete.ResultAsync()).Ok);
        Assert.False(File.Exists(DiskPath(image.ImageUrl)));
    }

    [Fact]
    public async Task Replacing_the_cover_deletes_the_previous_file()
    {
        var (browser, venue) = await OwnVenueAsync();
        async Task<string> UploadCoverAsync()
        {
            var form = Http.Form(("Price", "1"), ("MaxGuestCount", "10"), ("IsOutdoor", "false"), ("Services", "DJ"))
                .WithFile("CoverPhoto", "kapak.png", TestData.Images.Png, "image/png");
            Assert.True((await (await browser.PutAsync($"/api/organizations/{venue.Id}", form)).ResultAsync()).Ok);
            return (await factory.WithDbAsync(db => db.Organizations.Where(o => o.Id == venue.Id).Select(o => o.CoverPhotoPath).SingleAsync()))!;
        }

        var first = await UploadCoverAsync();
        var second = await UploadCoverAsync();

        Assert.NotEqual(first, second);
        Assert.False(File.Exists(DiskPath(first)));
        Assert.True(File.Exists(DiskPath(second)));
    }

    [Fact]
    public async Task Never_deletes_files_outside_the_uploads_folder()
    {
        var (browser, venue) = await OwnVenueAsync();
        var decoy = Path.Combine(factory.UploadRoot, $"decoy-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(decoy, "silinmemeli");
        var image = await factory.WithDbAsync(async db =>
        {
            var row = new OrganizationImage
            {
                ImageUrl = $"/uploads/../{Path.GetFileName(decoy)}",
                OrganizationId = venue.Id,
                IsActivated = true,
                CreateDate = DateTime.UtcNow,
            };
            db.OrganizationImages.Add(row);
            await db.SaveChangesAsync();
            return row;
        });

        var delete = await browser.DeleteAsync($"/api/organization-images/{image.Id}");

        Assert.True((await delete.ResultAsync()).Ok);
        Assert.True(File.Exists(decoy));
    }
}
