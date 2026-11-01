using HappyDay.Application.Features.Queries.Organization.GetOrganizationWithImages;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.Organization.GetManagedOrganization;

/// <summary>Firma panelinin düzenleme ekranı için mekan detayı (yayında olmasa da).</summary>
public class GetManagedOrganizationQueryRequest : IRequest<GeneralResponse<GetOrganizationWithImagesResponse>>
{
    public Guid Id { get; set; }
}
