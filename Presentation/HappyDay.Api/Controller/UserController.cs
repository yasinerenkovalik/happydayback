using HappyDay.Api.Auth;
using HappyDay.Application.Features.Commands.User.CreateUser;
using HappyDay.Application.Features.Queries.AutLogin;
using HappyDay.Application.Features.Queries.Auth.ValidateToken;
using HappyDay.Application.Features.Queries.User.GetAllUser;
using HappyDay.Application.Features.Queries.User.GetByIdUser;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HappyDay.Api.Controller
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST api/users/login
        [EnableRateLimiting("Auth")]
        [HttpPost("login")]
        public async Task<GeneralResponse<AuthLoginQueryResponse>>  Login(AuthLoginQueryRequest request)
        {
            var res = await _mediator.Send(request);
            if (res.isSuccess && res.Data?.Tokens is { } tokens) AuthCookies.Write(HttpContext, tokens);
            return res;
        }

        // POST api/users — kayıt
        [EnableRateLimiting("PublicWrite")]
        [HttpPost]
        public async Task<GeneralResponse<CreateUserCommandResponse>>  Create(CreateUserCommandRequest request)
        {
            return await _mediator.Send(request);
        }

        // GET api/users
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<GeneralResponse<List<GetAllUserQueryResponse>>>  GetAll( )
        {
            return await _mediator.Send(new  GetAllUserQueryRequest());
        }

        // GET api/users/{id}
        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<GeneralResponse<GetByIdUserQueryResponse>>  GetById(Guid id)
        {
            return await _mediator.Send(new GetByIdUserQueryRequest { Id = id });
        }

        // GET api/users/validate-token?token=
        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken(string? token)
        {
            var result = await _mediator.Send(new ValidateTokenQuery(token));
            return result.IsValid ? Ok(result) : Unauthorized(result.Message);
        }
    }
}
