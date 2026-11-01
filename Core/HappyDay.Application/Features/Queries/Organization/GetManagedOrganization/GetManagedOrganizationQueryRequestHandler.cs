using HappyDay.Application.Features.Queries.Organization.GetOrganizationWithImages;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.Organization.GetManagedOrganization;

public class GetManagedOrganizationQueryRequestHandler
    : IRequestHandler<GetManagedOrganizationQueryRequest, GeneralResponse<GetOrganizationWithImagesResponse>>
{
    private readonly IOrganizationRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetManagedOrganizationQueryRequestHandler(IOrganizationRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<GetOrganizationWithImagesResponse>> Handle(GetManagedOrganizationQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetOrganizationWithImages(request.Id, publishedOnly: false);
        if (result == null)
        {
            return new GeneralResponse<GetOrganizationWithImagesResponse>
            {
                isSuccess = false,
                Message = MessageConstants.OrganizationNotFound
            };
        }

        // Firma yetkilisi yalnızca kendi mekanını düzenlemek için açabilir
        if (!_currentUser.CanManageCompany(result.CompanyId))
        {
            return new GeneralResponse<GetOrganizationWithImagesResponse>
            {
                isSuccess = false,
                Message = MessageConstants.Forbidden
            };
        }

        return new GeneralResponse<GetOrganizationWithImagesResponse>
        {
            isSuccess = true,
            Message = MessageConstants.OrganizationGet,
            Data = result
        };
    }
}
