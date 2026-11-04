using System.Globalization;

namespace HappyDay.Application.Features.Queries.Seo;

/// <summary>
/// Arama motorları ve paylaşım önizlemeleri için mekan başlığı ve açıklaması.
/// Frontend'deki src/lib/seo.ts aynı metni üretir; biri değişirse öbürü de değişmeli.
/// </summary>
public static class SeoText
{
    public const string SiteName = "Mutlu Günüm";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// "Urla Bağ Evi — Urla, İzmir Düğün Mekanı | Mutlu Günüm". Etkinlik adı aramada eşleşsin diye ilk (en önemli)
    /// etkinlik eklenir; mekan etkinlik seçmediyse "Urla Bağ Evi — Urla, İzmir | Mutlu Günüm".
    /// </summary>
    public static string VenueTitle(string title, string districtName, string cityName, IReadOnlyList<string> eventNames)
    {
        var place = Place(districtName, cityName);
        return eventNames.Count > 0
            ? $"{title} — {place} {eventNames[0]} Mekanı | {SiteName}"
            : $"{title} — {place} | {SiteName}";
    }

    /// <summary>"Urla, İzmir · Kır Bahçesi · Düğün, Nişan, Kına Gecesi · 300 kişiye kadar · 72.000 ₺. Bağların…" (en fazla 160 karakter)</summary>
    public static string VenueDescription(string? categoryName, string districtName, string cityName,
        IReadOnlyList<string> eventNames, int maxGuestCount, decimal price, string? description)
    {
        var facts = string.Join(" · ", new[]
        {
            Place(districtName, cityName),
            categoryName,
            eventNames.Count > 0 ? string.Join(", ", eventNames.Take(3)) : null,
            maxGuestCount > 0 ? $"{maxGuestCount.ToString("N0", Turkish)} kişiye kadar" : null,
            price > 0 ? $"{price.ToString("N0", Turkish)} ₺" : null,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var text = string.IsNullOrWhiteSpace(description) ? facts : $"{facts}. {Collapse(description)}";
        return Truncate(text, 160);
    }

    private static string Place(string districtName, string cityName) =>
        string.IsNullOrWhiteSpace(districtName) || districtName == cityName ? cityName : $"{districtName}, {cityName}";

    private static string Collapse(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Kelime ortasında kesmeden kısaltır</summary>
    public static string Truncate(string text, int max)
    {
        if (text.Length <= max) return text;
        var cut = text[..(max - 1)];
        var space = cut.LastIndexOf(' ');
        return (space > max / 2 ? cut[..space] : cut).TrimEnd(' ', ',', '.', '·') + "…";
    }
}
