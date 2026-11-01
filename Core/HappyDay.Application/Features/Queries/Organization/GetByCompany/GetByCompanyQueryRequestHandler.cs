using HappyDay.Application.Interface.Services;
using MapsterMapper;
using HappyDay.Application.Features.Queries.Organization.GetByIdOrganization;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.Organization.GetByCompany;

public class GetByCompanyQueryRequestHandler:IRequestHandler<GetByCompanyQueryRequest,GeneralResponse<List<GetByCompanyQueryResponse>>>
{
    private readonly IOrganizationRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetByCompanyQueryRequestHandler(IOrganizationRepository repository, IMapper mapper, ICurrentUserService currentUser)
    {
        _repository = repository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public  async Task<GeneralResponse<List<GetByCompanyQueryResponse>>> Handle(GetByCompanyQueryRequest request, CancellationToken cancellationToken)
    {
        // Uç herkese açık: onaysız ya da yayından kaldırılmış mekanları yalnızca firmanın kendisi ve admin görür
        var publishedOnly = !_currentUser.CanManageCompany(request.CompanyId);
        var response= await _repository.GetByCompany(request.CompanyId, publishedOnly);
        var mappedResponse = _mapper.Map<List<GetByCompanyQueryResponse>>(response);
        return new GeneralResponse<List<GetByCompanyQueryResponse>>()
        {
            Data = mappedResponse,
            isSuccess = true
        };
    }
}

