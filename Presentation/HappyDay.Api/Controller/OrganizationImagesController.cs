using HappyDay.Application.Features.Commands.Organization.CreateOrganization;
using HappyDay.Application.Features.Commands.OrganizationImages.CreateOrganizationImages;
using HappyDay.Application.Features.Commands.OrganizationImages.DeleteOrganizationImages;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/organization-images")]
    [ApiController]
    public class OrganizationImagesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrganizationImagesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/organization-images (form: OrganizationId, OrganizationImage)
        [Authorize(Roles = "Admin,Company")]
        [HttpPost]
        public async Task<GeneralResponse<CreateOrganizationImagesCommandResponse>> Create([FromForm] CreateOrganizationImagesCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // DELETE api/organization-images/{id}
        [Authorize(Roles = "Admin,Company")]
        [HttpDelete("{id:int}")]
        public async Task<GeneralResponse<DeleteOrganizationImagesCommandResponse>> Delete(int id)
        {
            var request = new DeleteOrganizationImagesCommandRequest { Id = id };
            return await _mediator.Send(request);
        }
    }
}
