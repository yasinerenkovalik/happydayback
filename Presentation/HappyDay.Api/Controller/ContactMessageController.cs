using Microsoft.AspNetCore.Authorization;
using HappyDay.Application.Features.Commands.ContactMessage.CreateContactMessage;
using HappyDay.Application.Features.Queries.ContactMessage.GetByCompanyContactMessage;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller
{
    [Route("api/contact-messages")]
    [ApiController]
    public class ContactMessageController : ControllerBase
    {
        private readonly IMediator  _mediator;

        public ContactMessageController(IMediator mediator)
        {
            _mediator = mediator;

        }

        // POST api/contact-messages — mekan sayfasından firmaya mesaj
        [EnableRateLimiting("PublicWrite")]
        [HttpPost]
        public async Task<GeneralResponse<CreateContactMessageCommanResponse>> Create([FromForm] CreateContactMessageCommanRequest request)
        {
            return await _mediator.Send(request);
        }


        // GET api/contact-messages — firma kendi mesajlarını, admin ?companyId= ile seçtiği firmanınkini görür
        [Authorize(Roles = "Admin,Company")]
        [HttpGet]
        public async Task<GeneralResponse<List<GetByCompanyContactMessageResponse>>> GetForCompany([FromQuery] GetByCompanyContactMessageRequest request)
        {
            return await _mediator.Send(request);
        }
    }
}
