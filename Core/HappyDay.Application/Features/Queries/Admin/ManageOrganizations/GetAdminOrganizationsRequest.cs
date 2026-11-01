using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.Admin.ManageOrganizations;

public class GetAdminOrganizationsRequest : IRequest<GeneralResponse<List<OrganizationAdminListItem>>>
{
    /// <summary>Mekan ya da firma adı</summary>
    public string? Search { get; set; }

    /// <summary>Published, Removed, Unapproved; boşsa hepsi</summary>
    public OrganizationAdminStatus? Status { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetAdminOrganizationsRequestHandler
    : IRequestHandler<GetAdminOrganizationsRequest, GeneralResponse<List<OrganizationAdminListItem>>>
{
    private readonly IOrganizationRepository _organizations;

    public GetAdminOrganizationsRequestHandler(IOrganizationRepository organizations)
    {
        _organizations = organizations;
    }

    public async Task<GeneralResponse<List<OrganizationAdminListItem>>> Handle(
        GetAdminOrganizationsRequest request, CancellationToken cancellationToken)
    {
        var page = request.PageNumber < 1 ? 1 : request.PageNumber;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        return new GeneralResponse<List<OrganizationAdminListItem>>
        {
            Data = await _organizations.GetAllForAdmin(request.Search, request.Status, (page - 1) * size, size),
            isSuccess = true,
            Message = "Mekan listesi getirildi."
        };
    }
}
