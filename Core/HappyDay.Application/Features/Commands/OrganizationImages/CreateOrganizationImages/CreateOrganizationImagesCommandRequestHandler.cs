using MapsterMapper;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using HappyDay.Domain.Entities;
using MediatR;

namespace HappyDay.Application.Features.Commands.OrganizationImages.CreateOrganizationImages
{
    public class CreateOrganizationImagesCommandRequestHandler : IRequestHandler<CreateOrganizationImagesCommandRequest, GeneralResponse<CreateOrganizationImagesCommandResponse>>
    {
        private readonly IOrganizationImagesRepository _organizationImagesRepository;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateOrganizationImagesCommandRequestHandler(IOrganizationImagesRepository organizationImagesRepository, IMapper mapper, IFileService fileService, IOrganizationRepository organizationRepository, ICurrentUserService currentUser)
        {
            _organizationImagesRepository = organizationImagesRepository;
            _mapper = mapper;
            _fileService = fileService;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task<GeneralResponse<CreateOrganizationImagesCommandResponse>> Handle(CreateOrganizationImagesCommandRequest request, CancellationToken cancellationToken)
        {
            // Görsel yalnızca firmanın kendi organizasyonuna yüklenebilir
            var targetOrganization = await _organizationRepository.GetByIdAsync(request.OrganizationId);
            if (targetOrganization == null)
            {
                return new GeneralResponse<CreateOrganizationImagesCommandResponse>
                {
                    isSuccess = false,
                    Message = Messages.MessageConstants.OrganizationNotFound
                };
            }
            if (!_currentUser.CanManageCompany(targetOrganization.CompanyId))
            {
                return new GeneralResponse<CreateOrganizationImagesCommandResponse>
                {
                    isSuccess = false,
                    Message = Messages.MessageConstants.Forbidden
                };
            }

            var imagePath = await _fileService.SaveFileAsync(request.OrganizationImage, "uploads/organization");
            var organization = new OrganizationImage()
            {
                ImageUrl = imagePath,
                OrganizationId = request.OrganizationId
                // Id verme! EF Core otomatik alsın.
            };


  

            // 3. Veritabanına kaydet
            await _organizationImagesRepository.AddAsync(organization);

            // 4. Başarılı cevap dön
            return new GeneralResponse<CreateOrganizationImagesCommandResponse>
            {
                isSuccess = true,
                Message = "Organizasyon görseli başarıyla yüklendi.",
                Data = new CreateOrganizationImagesCommandResponse
                {
                   
                }
            };
        }
    }
}
