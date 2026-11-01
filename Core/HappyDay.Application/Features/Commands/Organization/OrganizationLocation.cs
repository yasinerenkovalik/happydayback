using HappyDay.Application.Interface.Repository;

namespace HappyDay.Application.Features.Commands.Organization;

internal static class OrganizationLocation
{
    /// <summary>Kaydedilecek konum geçerliyse null, değilse kullanıcıya gösterilecek hata mesajı.</summary>
    public static async Task<string?> ValidateAsync(Domain.Entities.Organization organization, IDistrictRepository districts)
    {
        if (organization.Latitude is < -90 or > 90 || organization.Longitude is < -180 or > 180)
            return "Geçersiz konum. Haritadan bir nokta seçin.";

        var cityDistricts = await districts.GetDistrictsByCityIdAsync(organization.CityId);
        if (!cityDistricts.Any(d => d.Id == organization.DistrictId))
            return "Seçilen ilçe bu ile ait değil.";

        return null;
    }
}
