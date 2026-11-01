using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Organization.DeleteOrganization;

public class DeleteOrganizationCommandRequestHandler:IRequestHandler<DeleteOrganizationCommandRequest, GeneralResponse<DeleteOrganizationCommandResponse>>
{
    private readonly IOrganizationRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<DeleteOrganizationCommandRequest> _validator;
    private readonly ICurrentUserService _currentUser;

    public DeleteOrganizationCommandRequestHandler(IOrganizationRepository repository, IMapper mapper, IValidator<DeleteOrganizationCommandRequest> validator, ICurrentUserService currentUser)
    {
        _repository = repository;
        _mapper = mapper;
        _validator = validator;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<DeleteOrganizationCommandResponse>> Handle(DeleteOrganizationCommandRequest request, CancellationToken cancellationToken)
    {
        // Olmayan kayıtta repository Exception fırlatıp 500 döndürmesin
        var organization = await _repository.GetByIdAsync(request.Id);
        if (organization is null)
        {
            return new GeneralResponse<DeleteOrganizationCommandResponse>
            {
                isSuccess = false,
                Message = Messages.MessageConstants.OrganizationNotFound
            };
        }

        // Firma yetkilisi yalnızca kendi mekanını silebilir
        if (!_currentUser.CanManageCompany(organization.CompanyId))
        {
            return new GeneralResponse<DeleteOrganizationCommandResponse>
            {
                isSuccess = false,
                Message = Messages.MessageConstants.Forbidden
            };
        }

        await _repository.DeleteAsync(request.Id);
        return new GeneralResponse<DeleteOrganizationCommandResponse>
        {
            isSuccess = true,
            Message = Messages.MessageConstants.OrganizationDeleted
        };
    }
}