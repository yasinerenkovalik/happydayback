using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Xml.Linq;
using HappyDay.Application.Features.Queries.Seo;
using HappyDay.Application.Features.Queries.Seo.GetSitemap;
using HappyDay.Application.Features.Queries.Seo.GetVenuePreview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    /// <summary>
    /// Arama motorları ve bağlantı önizlemeleri için: sitemap.xml, robots.txt ve mekan önizleme sayfası.
    /// Frontend'in robots.txt'si bu sitemap'i gösterir; nginx paylaşım botlarını önizleme sayfasına yollar.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SeoController : ControllerBase
    {
        private static readonly XNamespace Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";

        // Türkçe harfler olduğu gibi kalsın (WebUtility.HtmlEncode "ü"yü &#252; yapar); & < > " ' yine kodlanır
        private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

        private readonly IMediator _mediator;

        public SeoController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET /sitemap.xml
        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<ContentResult> SitemapXml()
        {
            var entries = await _mediator.Send(new GetSitemapQueryRequest());
            var xml = new XDocument(new XDeclaration("1.0", "UTF-8", null),
                new XElement(Sitemap + "urlset", entries.Select(e => new XElement(Sitemap + "url",
                    new XElement(Sitemap + "loc", e.Url),
                    e.LastModified is { } modified ? new XElement(Sitemap + "lastmod", modified.ToString("yyyy-MM-dd")) : null))));

            return Content(xml.Declaration + "\n" + xml.Root, "application/xml; charset=utf-8");
        }

        // GET /robots.txt — API'nin kendisi aranmasın; görseller ve sitemap okunabilsin
        [HttpGet("/robots.txt")]
        public ContentResult RobotsTxt() =>
            Content("User-agent: *\nAllow: /uploads/\nAllow: /sitemap.xml\nDisallow: /\n", "text/plain; charset=utf-8");

        // GET /seo/mekan/{id} — WhatsApp, Instagram vb. için başlık, açıklama ve kapak görseli
        [HttpGet("/seo/mekan/{id:guid}")]
        [ResponseCache(Duration = 300)]
        public async Task<IActionResult> VenuePreview(Guid id)
        {
            var preview = await _mediator.Send(new GetVenuePreviewQueryRequest
            {
                Id = id,
                AssetBaseUrl = $"{Request.Scheme}://{Request.Host}",
            });
            if (preview == null) return NotFound();

            return Content(PreviewHtml(preview), "text/html; charset=utf-8");
        }

        private static string PreviewHtml(VenuePreview p)
        {
            static string E(string s) => Html.Encode(s);

            var html = new StringBuilder()
                .Append("<!doctype html>\n<html lang=\"tr\">\n<head>\n<meta charset=\"utf-8\">\n")
                .Append($"<title>{E(p.Title)}</title>\n")
                .Append($"<meta name=\"description\" content=\"{E(p.Description)}\">\n")
                .Append($"<link rel=\"canonical\" href=\"{E(p.Url)}\">\n")
                .Append("<meta property=\"og:type\" content=\"website\">\n")
                .Append($"<meta property=\"og:site_name\" content=\"{E(SeoText.SiteName)}\">\n")
                .Append("<meta property=\"og:locale\" content=\"tr_TR\">\n")
                .Append($"<meta property=\"og:title\" content=\"{E(p.Title)}\">\n")
                .Append($"<meta property=\"og:description\" content=\"{E(p.Description)}\">\n")
                .Append($"<meta property=\"og:url\" content=\"{E(p.Url)}\">\n");
            if (p.ImageUrl != null)
                html.Append($"<meta property=\"og:image\" content=\"{E(p.ImageUrl)}\">\n")
                    .Append($"<meta property=\"og:image:alt\" content=\"{E(p.Heading)}\">\n")
                    .Append("<meta name=\"twitter:card\" content=\"summary_large_image\">\n");
            else
                html.Append("<meta name=\"twitter:card\" content=\"summary\">\n");

            return html
                .Append("</head>\n<body>\n")
                .Append($"<h1>{E(p.Heading)}</h1>\n<p>{E(p.Description)}</p>\n")
                .Append($"<p><a href=\"{E(p.Url)}\">{E(p.Url)}</a></p>\n")
                .Append("</body>\n</html>\n")
                .ToString();
        }
    }
}
