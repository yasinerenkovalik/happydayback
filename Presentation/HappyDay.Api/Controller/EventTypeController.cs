using HappyDay.Application.Common;
using HappyDay.Application.Features.Queries.EventType.GetEventTypes;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/event-types")]
    [ApiController]
    public class EventTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/event-types
        [HttpGet]
        [ResponseCache(Duration = 3600)]
        public async Task<GeneralResponse<List<EventTypes.EventType>>> GetAll()
        {
            return await _mediator.Send(new GetEventTypesQueryRequest());
        }
    }
}
