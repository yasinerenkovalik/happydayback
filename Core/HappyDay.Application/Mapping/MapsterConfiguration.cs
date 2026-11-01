using Mapster;

namespace HappyDay.Application.Mapping;

/// <summary>
/// Mapster ayarları. Eşlemeler isim eşleşmesiyle yapılır; buradaki varsayılanlar
/// AutoMapper'dan geçerken davranışın aynı kalması için seçildi.
/// Özel bir eşleme gerekirse bu assembly'de IRegister uygulayan bir sınıf eklemek yeterli.
/// </summary>
public static class MapsterConfiguration
{
    public static TypeAdapterConfig Create()
    {
        var config = new TypeAdapterConfig();

        config.Default
            // AutoMapper gibi alan adlarını büyük/küçük harf duyarsız eşle
            .NameMatchingStrategy(NameMatchingStrategy.IgnoreCase)
            // Aynı tipteki nesneleri kopyalamak yerine referansı taşı (EF navigation döngülerine karşı)
            .ShallowCopyForSameType(true)
            // AutoMapper gibi null koleksiyonu boş listeye çevir (ör. Organization.Services)
            .AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

        config.Scan(typeof(MapsterConfiguration).Assembly);

        return config;
    }
}
