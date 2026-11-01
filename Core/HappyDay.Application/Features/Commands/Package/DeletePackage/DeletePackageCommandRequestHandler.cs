using MapsterMapper;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Package.DeletePackage;

public class DeletePackageCommandRequestHandler : IRequestHandler<DeletePackageCommandRequest, GeneralResponse<DeletePackageCommandResponse>>
{
    private readonly IPackageRepository _repository;
    private readonly IMapper _mapper;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICurrentUserService _currentUser;

    public DeletePackageCommandRequestHandler(IPackageRepository repository, IMapper mapper, IOrganizationRepository organizationRepository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _mapper = mapper;
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<DeletePackageCommandResponse>> Handle(DeletePackageCommandRequest request, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(request.Id);
        if (package == null)
        {
            return new GeneralResponse<DeletePackageCommandResponse>
            {
                isSuccess = false,
                Message = "Paket bulunamadı."
            };
        }

        var organization = await _organizationRepository.GetByIdAsync(package.OrganizationId);
        if (!_currentUser.CanManageCompany(organization?.CompanyId))
        {
            return new GeneralResponse<DeletePackageCommandResponse>
            {
                isSuccess = false,
                Message = MessageConstants.Forbidden
            };
        }

        await _repository.DeleteAsync(package.Id);

        // Yanıt sınıfı şu an boş; Mapster alanı olmayan tipe eşleme yapmadığı için doğrudan oluşturuluyor
        var response = new DeletePackageCommandResponse();

        return new GeneralResponse<DeletePackageCommandResponse>
        {
            isSuccess = true,
            Message = "Paket başarıyla silindi.",
            Data = response
        };
    }
}