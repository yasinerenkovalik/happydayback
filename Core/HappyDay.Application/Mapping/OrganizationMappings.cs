using HappyDay.Application.Features.Commands.Organization.UpdateOrganization;
using HappyDay.Application.Features.Queries.Organization.GetByCompany;
using HappyDay.Domain.Entities;
using Mapster;

namespace HappyDay.Application.Mapping;

/// <summary>
/// Organizasyon güncellemesinde istekte gönderilmeyen (null) zorunlu alanlar mevcut değerini korur.
/// Aksi halde CategoryId 0'a, Title/Description/Duration null'a düşüp kayıt veritabanında hata veriyordu.
/// İsteğe bağlı alanlar (ReservationNote, CancelPolicy, VideoUrl) boş gönderilerek temizlenebilir.
/// </summary>
public class OrganizationMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateOrganizationCommandRequest, Organization>()
            .IgnoreIf((src, dest) => src.Title == null, dest => dest.Title)
            .IgnoreIf((src, dest) => src.Description == null, dest => dest.Description)
            .IgnoreIf((src, dest) => src.CategoryId == null, dest => dest.CategoryId)
            .IgnoreIf((src, dest) => src.Duration == null, dest => dest.Duration)
            .IgnoreIf((src, dest) => src.CityId == null, dest => dest.CityId)
            .IgnoreIf((src, dest) => src.DistrictId == null, dest => dest.DistrictId)
            .IgnoreIf((src, dest) => src.Adress == null, dest => dest.Adress)
            .IgnoreIf((src, dest) => src.Latitude == null, dest => dest.Latitude)
            .IgnoreIf((src, dest) => src.Longitude == null, dest => dest.Longitude)
            // Handler denetleyip yazar (EventTypes.Normalize); gönderilmezse mevcut seçim kalır
            .Ignore(dest => dest.EventTypeIds);

        // Firma panelindeki mekan listesinde "İlçe, İl" (GetByCompany City/District'i yükler)
        config.NewConfig<Organization, GetByCompanyQueryResponse>()
            .Map(dest => dest.Location,
                src => src.City != null && src.District != null ? src.District.DistrictName + ", " + src.City.CityName : null);
    }
}
