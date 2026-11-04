using MapsterMapper;
using HappyDay.Application.Common;
using HappyDay.Application.Features.Commands.Organization.UpdateOrganization;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Organization.UpdateOrganization
{
    public class UpdateOrganizationCommandRequestHandler : IRequestHandler<UpdateOrganizationCommandRequest, GeneralResponse<UpdateOrganizationCommandResponse>>
    {
        private readonly IOrganizationRepository _repository;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDistrictRepository _districts;

        public UpdateOrganizationCommandRequestHandler(IOrganizationRepository repository, IMapper mapper, IFileService fileService, ICurrentUserService currentUser, IDistrictRepository districts)
        {
            _districts = districts;
            _repository = repository;
            _mapper = mapper;
            _fileService = fileService;
            _currentUser = currentUser;
        }

        public async Task<GeneralResponse<UpdateOrganizationCommandResponse>> Handle(UpdateOrganizationCommandRequest request, CancellationToken cancellationToken)
        {
            var organization = await _repository.GetByIdAsync(request.Id);
            if (organization == null)
            {
                return new GeneralResponse<UpdateOrganizationCommandResponse>
                {
                    Message = Messages.MessageConstants.InvalidOrganizationData,
                    isSuccess = false
                };
            }

            if (!_currentUser.CanManageCompany(organization.CompanyId))
            {
                return new GeneralResponse<UpdateOrganizationCommandResponse>
                {
                    Message = Messages.MessageConstants.Forbidden,
                    isSuccess = false
                };
            }

            // Konum değiştiyse kapak yüklenmeden önce kontrol et
            _mapper.Map(request, organization);
            var locationError = await OrganizationLocation.ValidateAsync(organization, _districts);
            if (locationError is not null)
            {
                return new GeneralResponse<UpdateOrganizationCommandResponse>
                {
                    Message = locationError,
                    isSuccess = false
                };
            }

            if (request.EventTypeIds is not null)
            {
                var (eventTypeIds, eventTypeError) = EventTypes.Normalize(request.EventTypeIds);
                if (eventTypeError is not null)
                {
                    return new GeneralResponse<UpdateOrganizationCommandResponse>
                    {
                        Message = eventTypeError,
                        isSuccess = false
                    };
                }
                organization.EventTypeIds = eventTypeIds;
            }

            // 1. Kapak fotoğrafı varsa kaydet
            string? replacedCover = null;
            if (request.CoverPhoto != null)
            {
                replacedCover = organization.CoverPhotoPath;
                var coverPath = await _fileService.SaveFileAsync(request.CoverPhoto, "uploads/cover");
                organization.CoverPhotoPath = coverPath;
            }

            // 3. Veritabanında güncelle
            await _repository.UpdateAsync(organization);

            // Kayıt yeni kapağı gösterdikten sonra eskisini sil
            _fileService.DeleteFile(replacedCover);

            return new GeneralResponse<UpdateOrganizationCommandResponse>
            {
                Message = Messages.MessageConstants.OrganizationUpdated,
                isSuccess = true,
              
            };
        }
    }
}
