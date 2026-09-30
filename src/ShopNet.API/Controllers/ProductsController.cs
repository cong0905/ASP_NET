using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Products.Commands;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Application.Features.Products.Queries;

namespace ShopNet.API.Controllers;

public class ProductsController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<PaginatedList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetProducts([FromQuery] GetProductsQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProductDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetProductById(int id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ProductDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateProduct(int id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(Result<ProductDto>.Failure("Id trong URL và Body không trùng khớp."));
        }

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var result = await Mediator.Send(new DeleteProductCommand(id));
        return HandleResult(result);
    }

    [HttpPost("{id:int}/reviews")]
    [Authorize]
    [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<ReviewDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddReview(int id, [FromBody] AddReviewRequest request)
    {
        var command = new AddReviewCommand(id, request.Rating, request.Comment);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet("{id:int}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<List<ReviewDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetReviews(int id)
    {
        var result = await Mediator.Send(new GetProductReviewsQuery(id));
        return HandleResult(result);
    }
}

public record AddReviewRequest(int Rating, string Comment);
