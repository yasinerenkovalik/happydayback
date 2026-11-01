using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.OrganizationImages.DeleteOrganizationImages;

public class DeleteOrganizationImagesCommandRequestHandler:IRequestHandler<DeleteOrganizationImagesCommandRequest, GeneralResponse<DeleteOrganizationImagesCommandResponse>>
{
    private readonly IOrganizationImagesRepository  _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileService _fileService;

    public DeleteOrganizationImagesCommandRequestHandler(IOrganizationImagesRepository repository, ICurrentUserService currentUser, IFileService fileService)
    {
        _repository = repository;
        _currentUser = currentUser;
        _fileService = fileService;
    }

    public async Task<GeneralResponse<DeleteOrganizationImagesCommandResponse>> Handle(DeleteOrganizationImagesCommandRequest request, CancellationToken cancellationToken)
    {
        var image = await _repository.GetWithOrganizationAsync(request.Id);
        if (image == null)
        {
            return new GeneralResponse<DeleteOrganizationImagesCommandResponse>
            {
                isSuccess = false,
                Message = MessageConstants.RegisterNotFound
            };
        }
        if (!_currentUser.CanManageCompany(image.Organization?.CompanyId))
        {
            return new GeneralResponse<DeleteOrganizationImagesCommandResponse>
            {
                isSuccess = false,
                Message = MessageConstants.Forbidden
            };
        }

      await  _repository.DeleteOrganizationImageAsync(request.Id);

        // Kayıt pasifleşti; dosya herkese açık adreste kalmasın
        _fileService.DeleteFile(image.ImageUrl);
        return new GeneralResponse<DeleteOrganizationImagesCommandResponse>
        {
            isSuccess = true,
            Message = "Organization image deleted",
            Data = new DeleteOrganizationImagesCommandResponse {}
        };
    }
}