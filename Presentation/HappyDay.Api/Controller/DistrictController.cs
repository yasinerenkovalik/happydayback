using HappyDay.Application.Features.Queries.District.GetAllDistrict;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/districts")]
    [ApiController]
    public class DistrictController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DistrictController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/districts/by-city/34
        [HttpGet("by-city/{cityId:int}")]
        public async Task<GeneralResponse<List<GetAllDisctrictByCityResponse>>> GetByCity(int cityId)
        {
            return await _mediator.Send(new GetAllDisctrictByCityRequest { CityId = cityId });
        }
    }
}
