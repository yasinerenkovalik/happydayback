using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Common;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Organization.CreateOrganization;

public class CreateOrganizationCommandRequestHandler:IRequestHandler<CreateOrganizationCommandRequest, GeneralResponse<CreateOrganizationCommandResponse>>
{
    private readonly IMapper _mapper;
    private readonly IOrganizationRepository  _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IValidator <CreateOrganizationCommandRequest> _validator;
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDistrictRepository _districts;

    public CreateOrganizationCommandRequestHandler(IMapper mapper, IOrganizationRepository repository, IValidator<CreateOrganizationCommandRequest> validator, ICompanyRepository companyRepository, IFileService fileService, ICurrentUserService currentUser, IDistrictRepository districts)
    {
        _districts = districts;
        _mapper = mapper;
        _repository = repository;
        _validator = validator;
        _companyRepository = companyRepository;
        _fileService = fileService;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<CreateOrganizationCommandResponse>> Handle(CreateOrganizationCommandRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return new GeneralResponse<CreateOrganizationCommandResponse>
            {
                Message = Messages.MessageConstants.InvalidOrganizationData,
                isSuccess = false
            };
        }

        // Firma yetkilisi yalnızca kendi firmasına organizasyon ekleyebilir
        if (!_currentUser.CanManageCompany(request.CompanyId))
        {
            return new GeneralResponse<CreateOrganizationCommandResponse>
            {
                Message = Messages.MessageConstants.Forbidden,
                isSuccess = false
            };
        }

        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null)
        {
            return new GeneralResponse<CreateOrganizationCommandResponse>
            {
                Message = Messages.MessageConstants.CompanyNotFound,
                isSuccess = false
            };
        }

        // ✅ Mapster ile ana bilgileri eşleştir
        var organization = _mapper.Map<Domain.Entities.Organization>(request);
        organization.OrganizationImages = new List<Domain.Entities.OrganizationImage>();

        // Konum gönderilmediyse firmanın konumu kullanılır
        organization.CityId = request.CityId ?? company.CityId;
        organization.DistrictId = request.DistrictId ?? company.DistrictId;
        organization.Adress = string.IsNullOrWhiteSpace(request.Adress) ? company.Adress : request.Adress.Trim();
        organization.Latitude = request.Latitude ?? company.Latitude;
        organization.Longitude = request.Longitude ?? company.Longitude;

        var locationError = await OrganizationLocation.ValidateAsync(organization, _districts);
        if (locationError is not null)
        {
            return new GeneralResponse<CreateOrganizationCommandResponse>
            {
                Message = locationError,
                isSuccess = false
            };
        }

        var (eventTypeIds, eventTypeError) = EventTypes.Normalize(request.EventTypeIds);
        if (eventTypeError is not null)
        {
            return new GeneralResponse<CreateOrganizationCommandResponse>
            {
                Message = eventTypeError,
                isSuccess = false
            };
        }
        organization.EventTypeIds = eventTypeIds;

      
        if (request.CoverPhoto != null)
        {
            var path = await _fileService.SaveFileAsync(request.CoverPhoto, "uploads/cover");
            organization.CoverPhotoPath = path;
        }
    
        if (request.Images != null && request.Images.Any())
        {
            var uploadedPaths = await _fileService.SaveFilesAsync(request.Images, "uploads");
            foreach (var path in uploadedPaths)
            {
               
                organization.OrganizationImages.Add(new Domain.Entities.OrganizationImage
                {
                    ImageUrl = path,
                    IsActivated = true,
                    CreateDate = DateTime.UtcNow
                });
            }
        }

        await _repository.AddAsync(organization);

        return new GeneralResponse<CreateOrganizationCommandResponse>
        {
            Message = Messages.MessageConstants.OrganizationCreated,
            isSuccess = true,
            Data = new CreateOrganizationCommandResponse { Id = organization.Id }
        };
    }

}