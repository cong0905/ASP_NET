using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Orders.Commands;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Application.Features.Orders.Queries;
using ShopNet.Domain.Enums;

namespace ShopNet.API.Controllers;

[Authorize]
public class OrdersController : ApiControllerBase
{
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Checkout([FromBody] CheckoutCommand command)
    {
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedList<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetOrders([FromQuery] GetOrdersQuery query)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetOrderById(int id)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id));
        return HandleResult(result);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] OrderStatus newStatus)
    {
        var result = await Mediator.Send(new UpdateOrderStatusCommand(id, newStatus));
        return HandleResult(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CancelOrder(int id, [FromBody] string? reason = null)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id, reason));
        return HandleResult(result);
    }

    [HttpPost("{id:int}/pay")]
    [Authorize]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PayOrder(int id, [FromBody] PaymentMethod paymentMethod = PaymentMethod.VNPay_Mock)
    {
        var result = await Mediator.Send(new ProcessPaymentCommand(id, paymentMethod));
        return HandleResult(result);
    }
}
