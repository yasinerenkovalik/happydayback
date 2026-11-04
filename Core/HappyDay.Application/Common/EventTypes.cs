namespace HappyDay.Application.Common;

/// <summary>
/// Mekanın uygun olduğu etkinlikler (düğün, nişan, doğum günü...). Firma mekan eklerken seçer, site bunlara göre filtreler.
/// Kimlikler mekan kayıtlarında (Organizations.EventTypeIds) saklanır: mevcut kimliği değiştirmeyin ya da silmeyin,
/// yalnızca yenisini ekleyin. Slug sayfa adreslerinde kullanılır (?etkinlik=dugun).
/// </summary>
public static class EventTypes
{
    public record EventType(int Id, string Name, string Slug);

    /// <summary>Sitede bu sırayla görünür</summary>
    public static readonly IReadOnlyList<EventType> All =
    [
        new(1, "Düğün", "dugun"),
        new(2, "Nişan", "nisan"),
        new(3, "Kına Gecesi", "kina-gecesi"),
        new(4, "Söz", "soz"),
        new(5, "Nikah", "nikah"),
        new(6, "Bekarlığa Veda", "bekarliga-veda"),
        new(7, "Doğum Günü", "dogum-gunu"),
        new(8, "Sünnet", "sunnet"),
        new(9, "Baby Shower", "baby-shower"),
        new(10, "Mezuniyet", "mezuniyet"),
        new(11, "Kurumsal Etkinlik", "kurumsal-etkinlik"),
    ];

    public static EventType? FindBySlug(string slug) =>
        All.FirstOrDefault(e => string.Equals(e.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Kimlikleri katalog sırasıyla adlara çevirir; bilinmeyenleri atlar</summary>
    public static List<string> Names(IEnumerable<int> ids)
    {
        var set = ids.ToHashSet();
        return All.Where(e => set.Contains(e.Id)).Select(e => e.Name).ToList();
    }

    /// <summary>
    /// Mekan kaydı için seçimi denetler: en az bir, hepsi bilinen etkinlik. Geçerliyse tekrarsız ve sıralı listeyi,
    /// değilse kullanıcıya gösterilecek hatayı döner.
    /// </summary>
    public static (List<int> Ids, string? Error) Normalize(IEnumerable<int>? ids)
    {
        var distinct = (ids ?? []).Distinct().Order().ToList();
        if (distinct.Count == 0)
            return (distinct, "Mekanın uygun olduğu en az bir etkinlik seçin (düğün, nişan, doğum günü...).");
        if (distinct.Any(id => All.All(e => e.Id != id)))
            return (distinct, "Geçersiz etkinlik türü.");
        return (distinct, null);
    }
}
