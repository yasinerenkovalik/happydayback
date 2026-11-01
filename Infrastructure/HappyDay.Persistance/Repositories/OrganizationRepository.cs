using HappyDay.Application.Common;
using HappyDay.Application.Features.Queries.Organization.GetByCompany;
using HappyDay.Application.Features.Queries.Organization.GetFeatured;
using HappyDay.Application.Features.Queries.Organization.GetFilterOrganization;
using HappyDay.Application.Features.Queries.Organization.GetOrganizationWithImages;
using HappyDay.Application.Features.Queries.Seo.GetSitemap;
using HappyDay.Application.Interface.Repository;
using HappyDay.Domain.Entities;
using HappyDay.Persistance.Context;
using Microsoft.EntityFrameworkCore;

namespace HappyDay.Persistance.Repositories;

public class OrganizationRepository:GenericRepository<Organization>,IOrganizationRepository
{
    private readonly HappyDayContext _context;
    public OrganizationRepository(HappyDayContext appContext) : base(appContext)
    {
        _context = appContext;
    }

    public async Task<GetOrganizationWithImagesResponse> GetOrganizationWithImages(Guid Id, bool publishedOnly = true)
    {
        var query = _context.Organizations.Where(o => o.Id == Id && o.IsActivated);
        if (publishedOnly)
            query = query.Where(OrganizationVisibility.Published);

        var result = await query
            .Select(o => new GetOrganizationWithImagesResponse
            {
                Id = o.Id,
                Title = o.Title,
                Description = o.Description,
                Price = o.Price,
                MaxGuestCount = o.MaxGuestCount,
                CategoryId = o.CategoryId,
                CompanyId = o.CompanyId,
                CompanyName = o.Company.Name,
                CompanyAddress = o.Company.Adress,
                CityId = o.CityId,
                CityName = o.City.CityName,
                DistrictId = o.DistrictId,
                DistrictName = o.District.DistrictName,
                Adress = o.Adress,
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                Images = o.OrganizationImages
                    .Where(img => img.IsActivated == true)
                    .Select(img => new OrganizationImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl
                    }).ToList(),
                Duration = o.Duration,
                Services = o.Services,
                IsOutdoor = o.IsOutdoor,
                ReservationNote = o.ReservationNote,
                CancelPolicy = o.CancelPolicy,
                VideoUrl = o.VideoUrl,
                CoverPhotoPath = o.CoverPhotoPath,
                RemovedByAdminAt = o.RemovedByAdminAt,
                RemovalReason = o.RemovalReason,
            })
            .FirstOrDefaultAsync();

        return result;
    }

  public async Task<PagedResult<GetFilteredOrganizationsQueryResponse>> GetFilteredAsync(
    GetFilteredOrganizationsQueryRequest request,
    CancellationToken ct)
{
    // Herkese acik listeye yalnizca YAYINDA olan mekanlar girer:
    // mekan aktif + sirket aktif + sirket onayli.
    var query = _context.Organizations
        .Where(OrganizationVisibility.Published)
        .AsNoTracking()
        .AsQueryable();

    if (request.CityId.HasValue)
        query = query.Where(x => x.CityId == request.CityId);

    if (request.DistrictId.HasValue)
        query = query.Where(x => x.DistrictId == request.DistrictId);

    if (request.CategoryId.HasValue)
        query = query.Where(x => x.CategoryId == request.CategoryId);

    if (request.IsOutdoor.HasValue)
        query = query.Where(x => x.IsOutdoor == request.IsOutdoor);

    if (request.MaxPrice.HasValue)
        query = query.Where(x => x.Price <= request.MaxPrice);

    if (request.MinCapacity.HasValue)
        query = query.Where(x => x.MaxGuestCount >= request.MinCapacity);

    if (request.MaxCapacity.HasValue)
        query = query.Where(x => x.MaxGuestCount <= request.MaxCapacity);

    if (!string.IsNullOrWhiteSpace(request.Service))
    {
        var service = request.Service.Trim().ToLower();
        query = query.Where(x => x.Services.Any(s => s.ToLower().Contains(service)));
    }

    // 1) toplam kayıt
    var totalCount = await query.CountAsync(ct);

    // 2) sıralama (fiyata göre veya default olarak tarihe göre)
    if (request.SortByPriceAsc.HasValue)
    {
        if (request.SortByPriceAsc.Value)
            query = query.OrderBy(x => x.Price);
        else
            query = query.OrderByDescending(x => x.Price);
    }
    else
    {
        query = query.OrderByDescending(x => x.CreateDate);
    }

    // 3) sayfalama
    var skip = (request.Page - 1) * request.PageSize;

    var items = await query
        .Skip(skip)
        .Take(request.PageSize)
        .Select(x => new GetFilteredOrganizationsQueryResponse
        {
            Id = x.Id,
            Title = x.Title,
            CategoryId = x.CategoryId,
            Description = x.Description,
            Price = x.Price,
            MaxGuestCount = x.MaxGuestCount,
            Services = x.Services,
            Duration = x.Duration,
            IsOutdoor = x.IsOutdoor,
            ReservationNote = x.ReservationNote,
            CancelPolicy = x.CancelPolicy,
            VideoUrl = x.VideoUrl,
            CompanyId = x.CompanyId,
            CityId = x.CityId,
            CityName = x.City.CityName,
            DistrictId = x.DistrictId,
            DistrictName = x.District.DistrictName,
            Adress = x.Adress,
            CoverPhotoPath = x.CoverPhotoPath,
            Longitude = x.Longitude,
            Latitude = x.Latitude,
        })
        .ToListAsync(ct);

    return new PagedResult<GetFilteredOrganizationsQueryResponse>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}



    public Task<Organization?> GetPublishedByIdAsync(Guid id)
    {
        return _context.Organizations
            .Include(o => o.Company)
            .Include(o => o.Packages)
            .Include(o => o.OrganizationImages)
            .Where(OrganizationVisibility.Published)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public Task<int> CountPublishedByCityAsync(int cityId)
    {
        return _context.Organizations
            .Where(OrganizationVisibility.Published)
            .CountAsync(o => o.CityId == cityId);
    }

    public Task<int> CountPublishedAsync()
    {
        return _context.Organizations.Where(OrganizationVisibility.Published).CountAsync();
    }

    public Task<List<SitemapVenue>> GetPublishedForSitemapAsync()
    {
        return _context.Organizations
            .Where(OrganizationVisibility.Published)
            .OrderByDescending(o => o.CreateDate)
            .Select(o => new SitemapVenue(o.Id, o.UpdateDate > o.CreateDate ? o.UpdateDate : o.CreateDate))
            .ToListAsync();
    }

    public async Task<List<Organization>> GetByCompany(Guid companyId, bool publishedOnly)
    {
        // Firma paneli kendi kayıtlarını onay ve moderasyon durumundan bağımsız görür; ziyaretçi yalnızca yayındakileri
        var query = _context.Organizations
            .Include(o => o.City)
            .Include(o => o.District)
            .Where(o => o.CompanyId == companyId && o.IsActivated);
        if (publishedOnly)
            query = query.Where(OrganizationVisibility.Published);
        return await query.ToListAsync();
    }

   
    public async Task<List<Organization>> GetFeaturedAsync(GetFeaturedQueryRequest  request)
    {
        return await _context.Organizations
            .Where(OrganizationVisibility.Published)
            .Where(o => o.IsFeatured && o.CategoryId == request.Id)
            .ToListAsync();
    }

    public async Task<List<OrganizationAdminListItem>> GetAllForAdmin(string? search, OrganizationAdminStatus? status, int skip, int take)
    {
        var query = _context.Organizations.AsNoTracking().Where(o => o.IsActivated && o.Company.IsActivated);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o => o.Title.ToLower().Contains(s) || o.Company.Name.ToLower().Contains(s));
        }

        query = status switch
        {
            OrganizationAdminStatus.Published => query.Where(OrganizationVisibility.Published),
            OrganizationAdminStatus.Removed => query.Where(o => o.RemovedByAdminAt != null),
            OrganizationAdminStatus.Unapproved => query.Where(o => o.RemovedByAdminAt == null && !o.Company.IsApproved),
            _ => query,
        };

        return await query
            .OrderByDescending(o => o.CreateDate)
            .Skip(skip)
            .Take(take)
            .Select(o => new OrganizationAdminListItem
            {
                Id = o.Id,
                Title = o.Title,
                CompanyId = o.CompanyId,
                CompanyName = o.Company.Name,
                CompanyApproved = o.Company.IsApproved,
                CityName = o.City.CityName,
                DistrictName = o.District.DistrictName,
                CoverPhotoPath = o.CoverPhotoPath,
                Price = o.Price,
                CreateDate = o.CreateDate,
                RemovedByAdminAt = o.RemovedByAdminAt,
                RemovalReason = o.RemovalReason,
            })
            .ToListAsync();
    }
}
