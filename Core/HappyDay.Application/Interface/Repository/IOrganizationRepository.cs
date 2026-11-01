using HappyDay.Application.Features.Queries.Organization.GetByCompany;
using HappyDay.Application.Features.Queries.Organization.GetFeatured;
using HappyDay.Application.Features.Queries.Organization.GetFilterOrganization;
using HappyDay.Application.Features.Queries.Organization.GetOrganizationWithImages;
using HappyDay.Domain.Entities;

namespace HappyDay.Application.Interface.Repository;

public interface IOrganizationRepository:IGenericRepository<Organization>
{
    Task<List<Organization>> GetFeaturedAsync(GetFeaturedQueryRequest request);
    /// <summary>
    /// Mekan detayı ve görselleri. publishedOnly true ise yalnızca yayındaki (firması onaylı) mekan döner;
    /// firma paneli kendi mekanını onay durumundan bağımsız görmek için false verir.
    /// </summary>
    Task<GetOrganizationWithImagesResponse> GetOrganizationWithImages(Guid Id, bool publishedOnly = true);
    Task<List<Organization>> GetByCompany(Guid companyId, bool publishedOnly);

    /// <summary>Admin paneli: bütün aktif mekanlar, firma ve yayın durumuyla</summary>
    Task<List<OrganizationAdminListItem>> GetAllForAdmin(string? search, OrganizationAdminStatus? status, int skip, int take);
    
    Task<PagedResult<GetFilteredOrganizationsQueryResponse>> GetFilteredAsync(
        GetFilteredOrganizationsQueryRequest request,
        CancellationToken ct);

    /// <summary>Yayinda olan mekani getirir (aktif + sirketi aktif ve onayli).</summary>
    Task<Organization?> GetPublishedByIdAsync(Guid id);

    /// <summary>Herkese acik listelerde gorunmesi gereken (aktif + sirketi onayli) mekanlar.</summary>
    Task<int> CountPublishedByCityAsync(int cityId);

    Task<int> CountPublishedAsync();
   

    
}

public enum OrganizationAdminStatus
{
    /// <summary>Sitede görünüyor</summary>
    Published,
    /// <summary>Yönetici yayından kaldırdı</summary>
    Removed,
    /// <summary>Firması henüz onaylanmadığı için görünmüyor</summary>
    Unapproved,
}

/// <summary>Admin paneli mekan liste satırı.</summary>
public class OrganizationAdminListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = default!;
    public bool CompanyApproved { get; set; }
    public string CityName { get; set; } = default!;
    public string DistrictName { get; set; } = default!;
    public string? CoverPhotoPath { get; set; }
    public decimal Price { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? RemovedByAdminAt { get; set; }
    public string? RemovalReason { get; set; }
}
