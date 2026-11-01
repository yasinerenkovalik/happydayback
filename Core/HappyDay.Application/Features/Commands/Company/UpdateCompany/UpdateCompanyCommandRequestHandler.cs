using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Company.UpdateCompany
{
    public class UpdateCompanyCommandRequestHandler 
        : IRequestHandler<UpdateCompanyCommandRequest, GeneralResponse<UpdateCompanyCommandResponse>>
    {
        private readonly ICompanyRepository _repository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateCompanyCommandRequest> _validator;
        private readonly IFileService _fileService;
        private readonly ICurrentUserService _currentUser;

        public UpdateCompanyCommandRequestHandler(
            ICompanyRepository repository,
            IMapper mapper,
            IValidator<UpdateCompanyCommandRequest> validator, IFileService fileService,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _mapper = mapper;
            _validator = validator;
            _fileService = fileService;
            _currentUser = currentUser;
        }

        public async Task<GeneralResponse<UpdateCompanyCommandResponse>> Handle(
            UpdateCompanyCommandRequest request, 
            CancellationToken cancellationToken)
        {
            // 1) Validasyon
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
            {
                return new GeneralResponse<UpdateCompanyCommandResponse>
                {
                    isSuccess = false,
                    Message = validation.Errors.First().ErrorMessage
                };
            }

            // Firma yetkilisi yalnızca kendi firmasını güncelleyebilir
            if (!_currentUser.CanManageCompany(request.Id))
            {
                return new GeneralResponse<UpdateCompanyCommandResponse>
                {
                    Message = MessageConstants.Forbidden
                };
            }

            var existing = await _repository.GetByIdAsync(request.Id);
            if (existing is null)
            {
                return new GeneralResponse<UpdateCompanyCommandResponse>
                {
                    Message = MessageConstants.CompanyNotFound
                };
            }

            
            var replacedCover = existing.CoverPhotoPath;
            _mapper.Map(request, existing);
            if (request.CoverPhoto is not null)
            {
                existing.CoverPhotoPath = await _fileService.SaveFileAsync(request.CoverPhoto, "uploads/companycover");
            }
            await _repository.UpdateAsync(existing);

            // Kayıt yeni kapağı gösterdikten sonra eskisini sil
            if (request.CoverPhoto is not null)
            {
                _fileService.DeleteFile(replacedCover);
            }

            return new GeneralResponse<UpdateCompanyCommandResponse>
            {
                Message = MessageConstants.CompanyUpdated,
                isSuccess = true
              
            };
        }
    }
}
