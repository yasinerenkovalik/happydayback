using HappyDay.Application.Common;
using HappyDay.Application.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace HappyDay.Application.Features.Queries.Seo.GetVenuePreview;

/// <summary>
/// WhatsApp, Instagram gibi uygulamaların bağlantı önizlemesi için mekan bilgisi.
/// Bu uygulamalar JavaScript çalıştırmaz; frontend'in nginx'i onları API'nin hazırladığı sayfaya yönlendirir.
/// Yalnızca yayındaki mekan döner; yoksa null.
/// </summary>
public class GetVenuePreviewQueryRequest : IRequest<VenuePreview?>
{
    public Guid Id { get; set; }

    /// <summary>Yüklenen görsellerin sunulduğu API adresi (https://api.mutlugunum.com); göreli kapak yolu buna eklenir</summary>
    public string AssetBaseUrl { get; set; } = string.Empty;
}

public record VenuePreview(string Title, string Heading, string Description, string Url, string? ImageUrl);

public class GetVenuePreviewQueryRequestHandler : IRequestHandler<GetVenuePreviewQueryRequest, VenuePreview?>
{
    private readonly IOrganizationRepository _organizations;
    private readonly ICategoryRepository _categories;
    private readonly IConfiguration _config;

    public GetVenuePreviewQueryRequestHandler(IOrganizationRepository organizations, ICategoryRepository categories, IConfiguration config)
    {
        _organizations = organizations;
        _categories = categories;
        _config = config;
    }

    public async Task<VenuePreview?> Handle(GetVenuePreviewQueryRequest request, CancellationToken cancellationToken)
    {
        var venue = await _organizations.GetOrganizationWithImages(request.Id);
        if (venue == null) return null;

        var category = (await _categories.GetAllAysnc()).FirstOrDefault(c => c.Id == venue.CategoryId)?.Name;
        var site = (_config["Frontend:BaseUrl"] ?? "https://mutlugunum.com").TrimEnd('/');
        var cover = venue.CoverPhotoPath ?? venue.Images?.FirstOrDefault()?.ImageUrl;
        var events = EventTypes.Names(venue.EventTypeIds);

        return new VenuePreview(
            SeoText.VenueTitle(venue.Title, venue.DistrictName, venue.CityName, events),
            venue.Title,
            SeoText.VenueDescription(category, venue.DistrictName, venue.CityName, events, venue.MaxGuestCount, venue.Price, venue.Description),
            $"{site}/mekan/{venue.Id}",
            AbsoluteImageUrl(cover, request.AssetBaseUrl));
    }

    /// <summary>Tam adresse olduğu gibi (örnek mekanlar), göreli yolsa API adresiyle (frontend'deki uploadUrl gibi)</summary>
    private static string? AbsoluteImageUrl(string? path, string assetBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        // Uri.TryCreate kullanılmıyor: Linux'ta "/uploads/a.png" de file:// adresi sayılır
        if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return path;
        return path.Contains(':') ? null : $"{assetBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }
}
