using HappyDay.Api.Auth;
using HappyDay.Application.Features.Commands.Company.CreateCompany;
using HappyDay.Application.Features.Commands.Company.DeleteCompany;
using HappyDay.Application.Features.Commands.Company.UpdateCompany;
using HappyDay.Application.Features.Commands.Auth.ConfirmEmail;
using HappyDay.Application.Features.Commands.Auth.ResendConfirmation;
using HappyDay.Application.Features.PasswordReset.RequestPasswordReset;
using HappyDay.Application.Features.PasswordReset.ResetPassword;
using HappyDay.Application.Features.Queries.Auth.OrganizationLogin;
using HappyDay.Application.Features.Queries.Company.GetAllCompany;
using HappyDay.Application.Features.Queries.Company.GetByIdCompany;
using HappyDay.Application.Wrappers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller
{
    [Route("api/companies")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/companies — şirket kaydı (CreateCompany handler içinde e-posta onayı tetikleniyor)
        [EnableRateLimiting("PublicWrite")]
        [HttpPost]
        public async Task<GeneralResponse<CreateCompanyCommandResponse>> Create([FromForm] CreateCompanyCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // POST api/companies/login — giriş (email onayı yoksa handler uygun mesaj dönecek)
        [EnableRateLimiting("Auth")]
        [HttpPost("login")]
        [Consumes("application/json")]
        public async Task<GeneralResponse<CompanyLoginQueryResponse>> Login([FromBody] CompanyLoginQueryRequest request)
        {
            var res = await _mediator.Send(request);
            if (res.isSuccess && res.Data?.Tokens is { } tokens) AuthCookies.Write(HttpContext, tokens);
            return res;
        }

        // E-posta onayı (maildeki linkten FRONTEND çağırır; body: { companyId, token })
        [EnableRateLimiting("Auth")]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand request, CancellationToken ct)
        {
            var res = await _mediator.Send(request, ct);
            if (!res.isSuccess) return BadRequest(res);
            return Ok(res);
        }

        // Onay mailini tekrar gönder (UI'da "Onay maili gelmedi mi?" butonu)
        [EnableRateLimiting("PublicWrite")]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationCommand request, CancellationToken ct)
        {
            var res = await _mediator.Send(request, ct);
            // Bilgi sızdırmamak için her durumda 200 dönüyoruz (mesajdan anlaşılır)
            return Ok(res);
        }

        // PUT api/companies/{id}
        [Authorize(Roles = "Admin,Company")]
        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(Guid id, [FromForm] UpdateCompanyCommandRequest request)
        {
            request.Id = id;
            var res = await _mediator.Send(request);
            return Ok(res);
        }

        // DELETE api/companies/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<GeneralResponse<DeleteCompanyCommandResponse>> Delete(Guid id)
        {
            return await _mediator.Send(new DeleteCompanyCommandRequest { Id = id });
        }

        // GET api/companies/{id}
        [HttpGet("{id:guid}")]
        public async Task<GeneralResponse<GetByIdCompanyQueryResponse>> GetById(Guid id)
        {
            return await _mediator.Send(new GetByIdCompanyQueryRequest { Id = id });
        }

        // GET api/companies
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<GeneralResponse<List<GetAllCompanyQueryResponse>>> GetAll()
        {
            return await _mediator.Send(new GetAllCompanyQueryRequest());
        }
        [AllowAnonymous]
        [EnableRateLimiting("PublicWrite")]
        [HttpPost("request-password-reset")]
        public async Task<GeneralResponse<Unit>> RequestPasswordReset(
            [FromBody] RequestPasswordResetCommand req, CancellationToken ct)
            => await _mediator.Send(req, ct);

        [AllowAnonymous]
        [EnableRateLimiting("Auth")]
        [HttpPost("reset-password")]
        public async Task<GeneralResponse<Unit>> ResetPassword(
            [FromBody] ResetPasswordCommand req, CancellationToken ct)
            => await _mediator.Send(req, ct);
    }
}
