using HappyDay.Application.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace HappyDay.Application.Features.Queries.Seo.GetSitemap;

/// <summary>sitemap.xml: ana sayfa ve yayındaki bütün mekanların frontend adresleri</summary>
public class GetSitemapQueryRequest : IRequest<List<SitemapEntry>>;

public record SitemapEntry(string Url, DateTime? LastModified);

/// <summary>Repository'den gelen yayındaki mekan; LastModified, CreateDate ile UpdateDate'in yenisi</summary>
public record SitemapVenue(Guid Id, DateTime LastModified);

public class GetSitemapQueryRequestHandler : IRequestHandler<GetSitemapQueryRequest, List<SitemapEntry>>
{
    private readonly IOrganizationRepository _organizations;
    private readonly IConfiguration _config;

    public GetSitemapQueryRequestHandler(IOrganizationRepository organizations, IConfiguration config)
    {
        _organizations = organizations;
        _config = config;
    }

    public async Task<List<SitemapEntry>> Handle(GetSitemapQueryRequest request, CancellationToken cancellationToken)
    {
        var site = (_config["Frontend:BaseUrl"] ?? "https://mutlugunum.com").TrimEnd('/');
        var venues = await _organizations.GetPublishedForSitemapAsync();

        var entries = new List<SitemapEntry> { new($"{site}/", venues.Count > 0 ? venues.Max(v => v.LastModified) : null) };
        entries.AddRange(venues.Select(v => new SitemapEntry($"{site}/mekan/{v.Id}", v.LastModified)));
        return entries;
    }
}
