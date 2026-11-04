using HappyDay.Application.Common;
using HappyDay.Application.Wrappers;
using MediatR;

namespace HappyDay.Application.Features.Queries.EventType.GetEventTypes;

/// <summary>Mekanların seçebileceği etkinlik türleri, sitedeki sırasıyla</summary>
public class GetEventTypesQueryRequest : IRequest<GeneralResponse<List<EventTypes.EventType>>>;

public class GetEventTypesQueryRequestHandler : IRequestHandler<GetEventTypesQueryRequest, GeneralResponse<List<EventTypes.EventType>>>
{
    public Task<GeneralResponse<List<EventTypes.EventType>>> Handle(GetEventTypesQueryRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new GeneralResponse<List<EventTypes.EventType>>
        {
            Data = EventTypes.All.ToList(),
            isSuccess = true,
        });
}
