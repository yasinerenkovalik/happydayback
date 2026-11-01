using HappyDay.Application.Features.Commands.Organization.CreateOrganization;
using HappyDay.Application.Features.Commands.Organization.DeleteOrganization;
using HappyDay.Application.Features.Commands.Organization.UpdateOrganization;
using HappyDay.Application.Features.Queries.Organization.GetAllOrganization;
using HappyDay.Application.Features.Queries.Organization.GetByCompany;
using HappyDay.Application.Features.Queries.Organization.GetByIdOrganization;
using HappyDay.Application.Features.Queries.Organization.GetFeatured;
using HappyDay.Application.Features.Queries.Organization.GetFilterOrganization;
using HappyDay.Application.Features.Queries.Organization.GetManagedOrganization;
using HappyDay.Application.Features.Queries.Organization.GetOrganizationWithImages;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/organizations")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly IMediator  _mediator;
        public OrganizationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/organizations
        [Authorize(Roles = "Admin,Company")]
        [HttpPost]
        public async Task<GeneralResponse<CreateOrganizationCommandResponse>> Create([FromForm] CreateOrganizationCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // GET api/organizations?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<GeneralResponse<PagedResult<GetAllOrganizationQueryResponse>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await _mediator.Send(new GetAllOrganizationQueryRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        // GET api/organizations/{id}
        [HttpGet("{id:guid}")]
        public async Task<GeneralResponse<GetByIdOrganizationQueryResponse>> GetById(Guid id)
        {
            return await _mediator.Send(new GetByIdOrganizationQueryRequest { Id = id });
        }

        // GET api/organizations/{id}/with-images
        [HttpGet("{id:guid}/with-images")]
        public async Task<GeneralResponse<GetOrganizationWithImagesResponse>> GetWithImages(Guid id)
        {
            return await _mediator.Send(new GetOrganizationWithImagesQueryRequest { Id = id });
        }

        // GET api/organizations/{id}/manage — firma panelinin düzenleme ekranı; yayında olmasa da döner
        [Authorize(Roles = "Admin,Company")]
        [HttpGet("{id:guid}/manage")]
        public async Task<GeneralResponse<GetOrganizationWithImagesResponse>> GetForManagement(Guid id)
        {
            return await _mediator.Send(new GetManagedOrganizationQueryRequest { Id = id });
        }

        // GET api/organizations/by-company/{companyId}
        [HttpGet("by-company/{companyId:guid}")]
        public async Task<GeneralResponse<List<GetByCompanyQueryResponse>>> GetByCompany(Guid companyId)
        {
            return await _mediator.Send(new GetByCompanyQueryRequest { CompanyId = companyId });
        }

        // GET api/organizations/filter?...
        [HttpGet("filter")]
        public async Task<GeneralResponse<PagedResult<GetFilteredOrganizationsQueryResponse>>> GetFiltered([FromQuery] GetFilteredOrganizationsQueryRequest query)
        {
            return await _mediator.Send(query);
        }

        // GET api/organizations/featured?categoryId=1
        [HttpGet("featured")]
        public async Task<GeneralResponse<List<GetFeaturedQueryResponse>>> GetFeatured([FromQuery] int categoryId)
        {
            return await _mediator.Send(new GetFeaturedQueryRequest { Id = categoryId });
        }

        // PUT api/organizations/{id}
        [Authorize(Roles = "Admin,Company")]
        [HttpPut("{id:guid}")]
        public async Task<GeneralResponse<UpdateOrganizationCommandResponse>> Update(Guid id, [FromForm] UpdateOrganizationCommandRequest request)
        {
            request.Id = id;
            return await _mediator.Send(request);
        }

        // DELETE api/organizations/{id} — firma yalnızca kendi mekanını silebilir (handler'da kontrol)
        [Authorize(Roles = "Admin,Company")]
        [HttpDelete("{id:guid}")]
        public async Task<GeneralResponse<DeleteOrganizationCommandResponse>> Delete(Guid id)
        {
            return await _mediator.Send(new DeleteOrganizationCommandRequest { Id = id });
        }
    }
}
