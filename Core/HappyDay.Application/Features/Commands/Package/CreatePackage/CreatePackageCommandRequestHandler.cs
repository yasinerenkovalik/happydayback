using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Package.CreatePackage;

public class CreatePackageCommandRequestHandler: IRequestHandler<CreatePackageCommandRequest, GeneralResponse<CreatePackageCommandResponse>>
{
    private IValidator<CreatePackageCommandRequest>  _validator;
    private readonly IMapper _mapper;
    private readonly IPackageRepository  _repository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICurrentUserService _currentUser;

    public CreatePackageCommandRequestHandler(IPackageRepository repository, IValidator<CreatePackageCommandRequest>  validator, IMapper mapper, IOrganizationRepository organizationRepository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _validator = validator;
        _mapper = mapper;
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<CreatePackageCommandResponse>> Handle(CreatePackageCommandRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new GeneralResponse<CreatePackageCommandResponse>
            {
                Message = validationResult.Errors.First().ErrorMessage,
                isSuccess = false
            };
        }

        // Paket yalnızca firmanın kendi organizasyonuna eklenebilir
        var organization = await _organizationRepository.GetByIdAsync(request.OrganizationId);
        if (organization == null)
        {
            return new GeneralResponse<CreatePackageCommandResponse>
            {
                Message = MessageConstants.OrganizationNotFound,
                isSuccess = false
            };
        }
        if (!_currentUser.CanManageCompany(organization.CompanyId))
        {
            return new GeneralResponse<CreatePackageCommandResponse>
            {
                Message = MessageConstants.Forbidden,
                isSuccess = false
            };
        }

        var package = _mapper.Map<global::Package>(request);
        await _repository.AddAsync(package);
        return new GeneralResponse<CreatePackageCommandResponse>
        {
            Message = "Package created",
            isSuccess = true
        };
        
        
    }
}