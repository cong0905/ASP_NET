using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Categories.Commands;
using ShopNet.Application.Features.Categories.DTOs;
using ShopNet.Application.Features.Categories.Queries;

namespace ShopNet.API.Controllers;

public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCategories()
    {
        var result = await Mediator.Send(new GetCategoriesQuery());
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<CategoryDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
