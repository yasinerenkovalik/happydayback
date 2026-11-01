using HappyDay.Application.Features.Commands.Contact.CreateContact;
using HappyDay.Application.Features.Queries.Contact.GetAllContact;
using HappyDay.Application.Features.Queries.Contact.GetByIdContact;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller
{
    [Route("api/contacts")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ContactController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/contacts — iletişim formu
        [EnableRateLimiting("PublicWrite")]
        [HttpPost]
        public async Task<GeneralResponse<CreateContactCommandResponse>> Create([FromForm] CreateContactCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // İletişim formu kayıtları kişisel veri içerir (ad, e-posta, telefon); yalnızca Admin okuyabilir.
        // GET api/contacts
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<GeneralResponse<List<GetAllContactQueryResponse>>> GetAll()
        {
            return await _mediator.Send(new GetAllContactQueryRequest());
        }

        // GET api/contacts/{id}
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<GeneralResponse<GetByIdContactQueryResponse>> GetById(Guid id)
        {
            return await _mediator.Send(new GetByIdContactQueryRequest { Id = id });
        }
    }
}
