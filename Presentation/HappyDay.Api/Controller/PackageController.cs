using Microsoft.AspNetCore.Authorization;
using HappyDay.Application.Features.Commands.Package.CreatePackage;
using HappyDay.Application.Features.Commands.Package.UpdatePackage;
using HappyDay.Application.Features.Commands.Package.DeletePackage;
using HappyDay.Application.Features.Queries.Package.GetByCompany;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/packages")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PackageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/packages
        [Authorize(Roles = "Admin,Company")]
        [HttpPost]
        public async Task<GeneralResponse<CreatePackageCommandResponse>> Create([FromBody] CreatePackageCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // GET api/packages/by-organization/{organizationId}
        // Herkese açık: paketler ve fiyatları mekan detay sayfasında ziyaretçiye gösterilir.
        [AllowAnonymous]
        [HttpGet("by-organization/{organizationId:guid}")]
        public async Task<GeneralResponse<List<GetByOrganizastionPackageResponse>>> GetByOrganization(Guid organizationId)
        {
            return await _mediator.Send(new GetByOrganizastionPackageRequest { Id = organizationId });
        }

        // PUT api/packages/{id}
        [Authorize(Roles = "Admin,Company")]
        [HttpPut("{id:guid}")]
        public async Task<GeneralResponse<UpdatePackageCommandResponse>> Update(Guid id, [FromBody] UpdatePackageCommandRequest request)
        {
            request.Id = id;
            return await _mediator.Send(request);
        }

        // DELETE api/packages/{id}
        [Authorize(Roles = "Admin,Company")]
        [HttpDelete("{id:guid}")]
        public async Task<GeneralResponse<DeletePackageCommandResponse>> Delete(Guid id)
        {
            var request = new DeletePackageCommandRequest { Id = id };
            return await _mediator.Send(request);
        }
    }
}