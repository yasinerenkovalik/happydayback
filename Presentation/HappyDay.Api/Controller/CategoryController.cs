using HappyDay.Application.Features.Queries.Category.GetAllCategory;
using HappyDay.Application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyDay.Api.Controller
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET api/categories
        [HttpGet]
        public async Task<GeneralResponse<List<GetAllCategoryQueryResponse>>> GetAll()
        {
            return await _mediator.Send(new GetAllCategoryQueryRequest());
        }
    }
}
