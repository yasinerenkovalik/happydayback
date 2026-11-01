using HappyDay.Application.Features.Invitations;
using HappyDay.Application.Features.Invitations.RegisterByInvite;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller
{
    // ===========================
    // 1) ADMIN: Davetiye Oluştur
    // ===========================
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/invitations")]
    public class AdminInvitationsController : ControllerBase
    {
        private readonly ICompanyInvitationService _invitationService;

        public AdminInvitationsController(ICompanyInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        // POST api/admin/invitations
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvitationRequest req, CancellationToken ct)
        {
            return Ok(await _invitationService.CreateAsync(req, ct));
        }
    }

 
    [ApiController]
    [Route("api/companies")]
    public class CompanyAuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyAuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/companies/register-by-invite
        [AllowAnonymous]
        [EnableRateLimiting("Auth")]
        [HttpPost("register-by-invite")]
        public async Task<GeneralResponse<RegisterByInviteResponse>> RegisterByInvite(
            [FromBody] RegisterByInviteCommand req, CancellationToken ct)
            => await _mediator.Send(req, ct);
       
    }
}
