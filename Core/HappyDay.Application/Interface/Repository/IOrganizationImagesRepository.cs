using HappyDay.Domain.Entities;

namespace HappyDay.Application.Interface.Repository;

public interface IOrganizationImagesRepository:IGenericRepository<OrganizationImage>
{
    Task<OrganizationImage> DeleteOrganizationImageAsync(int id);

    /// <summary>Görseli bağlı olduğu organizasyonla birlikte getirir (sahiplik kontrolü için).</summary>
    Task<OrganizationImage?> GetWithOrganizationAsync(int id);
}