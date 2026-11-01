using MapsterMapper;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Package.UpdatePackage;

public class UpdatePackageCommandRequestHandler : IRequestHandler<UpdatePackageCommandRequest, GeneralResponse<UpdatePackageCommandResponse>>
{
    private readonly IPackageRepository _repository;
    private readonly IMapper _mapper;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICurrentUserService _currentUser;

    public UpdatePackageCommandRequestHandler(IPackageRepository repository, IMapper mapper, IOrganizationRepository organizationRepository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _mapper = mapper;
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<UpdatePackageCommandResponse>> Handle(UpdatePackageCommandRequest request, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(request.Id);
        if (package == null)
        {
            return new GeneralResponse<UpdatePackageCommandResponse>
            {
                isSuccess = false,
                Message = "Paket bulunamadı."
            };
        }

        var organization = await _organizationRepository.GetByIdAsync(package.OrganizationId);
        if (!_currentUser.CanManageCompany(organization?.CompanyId))
        {
            return new GeneralResponse<UpdatePackageCommandResponse>
            {
                isSuccess = false,
                Message = MessageConstants.Forbidden
            };
        }

        // request -> package map (entity güncelleme)
        _mapper.Map(request, package);

        await _repository.UpdateAsync(package);

        // Yanıt sınıfı şu an boş; Mapster alanı olmayan tipe eşleme yapmadığı için doğrudan oluşturuluyor
        var response = new UpdatePackageCommandResponse();

        return new GeneralResponse<UpdatePackageCommandResponse>
        {
            isSuccess = true,
            Message = "Paket başarıyla güncellendi.",
            Data = response
        };
    }
}