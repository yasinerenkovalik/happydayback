using MapsterMapper;
using HappyDay.Application.Interface.Repository;
using HappyDay.Application.Interface.Services;
using HappyDay.Application.Messages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.ContactMessage.GetByCompanyContactMessage;

public class GetByCompanyContactMessageRequestHandler 
    : IRequestHandler<GetByCompanyContactMessageRequest, GeneralResponse<List<GetByCompanyContactMessageResponse>>>
{
    private readonly IContactMessageRepository _contactMessageRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetByCompanyContactMessageRequestHandler(
        IContactMessageRepository contactMessageRepository,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _contactMessageRepository = contactMessageRepository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<GeneralResponse<List<GetByCompanyContactMessageResponse>>> Handle(
        GetByCompanyContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        // Firma yetkilisi yalnızca kendi mesajlarını görür; istemcinin gönderdiği CompanyId yok sayılır.
        // Admin hangi firmanın mesajlarını istediğini söylemek zorunda.
        var companyId = _currentUser.IsAdmin ? request.CompanyId : _currentUser.CompanyId;
        if (companyId is not { } id || id == Guid.Empty)
        {
            return new GeneralResponse<List<GetByCompanyContactMessageResponse>>
            {
                isSuccess = false,
                Message = _currentUser.IsAdmin ? "Admin için companyId zorunludur." : MessageConstants.Forbidden
            };
        }

        // Mesajları bağlı oldukları mekan bilgisiyle birlikte getir; mesaj yoksa boş liste
        var messages = await _contactMessageRepository.GetByCompanyWithVenue(id);

        var response = _mapper.Map<List<GetByCompanyContactMessageResponse>>(messages);

        return new GeneralResponse<List<GetByCompanyContactMessageResponse>>
        {
            Data = response,
            isSuccess = true,
            Message = "Şirkete ait mesajlar başarıyla getirildi."
        };
    }
}
