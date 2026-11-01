using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.ContactMessage.GetByCompanyContactMessage;

public class GetByCompanyContactMessageRequest:IRequest<GeneralResponse<List<GetByCompanyContactMessageResponse>>>
{
    /// <summary>Yalnızca admin için: hangi firmanın mesajları. Firma yetkilisinde yok sayılır, kendi firması kullanılır.</summary>
    public Guid? CompanyId { get; set; }
}
