using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Features.Orders.Commands;
using ShopNet.Domain.Enums;

namespace ShopNet.API.Controllers;

[Route("api/[controller]")]
public class PaymentsController : ApiControllerBase
{
    private readonly IVnPayService _vnPayService;

    public PaymentsController(IVnPayService vnPayService)
    {
        _vnPayService = vnPayService;
    }

    /// <summary>
    /// Tạo liên kết thanh toán (VNPay / MoMo) cho đơn hàng
    /// </summary>
    [HttpPost("create-payment-url")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var command = new CreatePaymentUrlCommand(request.OrderId, request.Provider, ipAddress);
        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tiếp nhận Return URL từ VNPay khi người dùng hoàn tất thanh toán và được redirect về
    /// </summary>
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var queryParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new ProcessPaymentCallbackCommand(PaymentMethod.VNPay, queryParams);
        var result = await Mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Tiếp nhận IPN Webhook ngầm từ VNPay (Server-to-Server)
    /// </summary>
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        var queryParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var command = new ProcessPaymentCallbackCommand(PaymentMethod.VNPay, queryParams);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }

        return Ok(new { RspCode = "99", Message = result.Message });
    }

    /// <summary>
    /// Tiếp nhận IPN Webhook ngầm từ MoMo (Server-to-Server)
    /// </summary>
    [HttpPost("momo-ipn")]
    public async Task<IActionResult> MoMoIpn([FromBody] MoMoIpnRequest request)
    {
        var command = new ProcessPaymentCallbackCommand(PaymentMethod.MoMo, new Dictionary<string, string>(), request);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BadRequest(new { message = result.Message });
    }
}

public record CreatePaymentUrlRequest(int OrderId, PaymentMethod Provider);
