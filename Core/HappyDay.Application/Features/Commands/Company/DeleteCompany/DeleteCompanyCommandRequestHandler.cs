using MapsterMapper;
using FluentValidation;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Commands.Company.DeleteCompany;

public class DeleteCompanyCommandRequestHandler(
    ICompanyRepository repository,
    IMapper mapper,
    IValidator<DeleteCompanyCommandRequest> validator,
    IAuthSessionRepository sessions)
    : IRequestHandler<DeleteCompanyCommandRequest, GeneralResponse<DeleteCompanyCommandResponse>>
{
    private readonly ICompanyRepository _repository = repository;
    private readonly IMapper _mapper = mapper;
    private readonly IValidator<DeleteCompanyCommandRequest> _validator = validator;

    public async Task<GeneralResponse<DeleteCompanyCommandResponse>> Handle(DeleteCompanyCommandRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return new GeneralResponse<DeleteCompanyCommandResponse>
            {
                isSuccess = false,
                Message = validationResult.Errors.First().ErrorMessage,
            };
        }

        // Olmayan kayıtta repository Exception fırlatıp 500 döndürmesin
        if (await _repository.GetByIdAsync(request.Id) is null)
        {
            return new GeneralResponse<DeleteCompanyCommandResponse>
            {
                isSuccess = false,
                Message = Messages.MessageConstants.CompanyNotFound
            };
        }

        await _repository.DeleteAsync(request.Id);
        await sessions.RevokeAllAsync(request.Id, "deleted");
        return new GeneralResponse<DeleteCompanyCommandResponse>()
        {
            isSuccess = true,
            Message = Messages.MessageConstants.CompanyDeleted
        };
    }
}