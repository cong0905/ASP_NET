using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Cart.Commands;
using ShopNet.Application.Features.Cart.DTOs;
using ShopNet.Application.Features.Cart.Queries;

namespace ShopNet.API.Controllers;

[Authorize]
public class CartController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetCart()
    {
        var result = await Mediator.Send(new GetCartQuery());
        return HandleResult(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddItem([FromBody] AddToCartCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpPut("items")]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateItem([FromBody] UpdateCartItemCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpDelete("items/{productId:int}")]
    [ProducesResponseType(typeof(Result<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> RemoveItem(int productId)
    {
        var result = await Mediator.Send(new RemoveCartItemCommand(productId));
        return HandleResult(result);
    }
}
