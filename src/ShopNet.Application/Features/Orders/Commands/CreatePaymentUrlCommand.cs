using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Orders.Commands;

public record CreatePaymentUrlCommand(
    int OrderId,
    PaymentMethod Provider,
    string? IpAddress = null
) : IRequest<Result<string>>;

public class CreatePaymentUrlCommandHandler : IRequestHandler<CreatePaymentUrlCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IVnPayService _vnPayService;
    private readonly IMoMoService _moMoService;
    private readonly ICurrentUserService _currentUserService;

    public CreatePaymentUrlCommandHandler(
        IApplicationDbContext context,
        IVnPayService vnPayService,
        IMoMoService moMoService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _vnPayService = vnPayService;
        _moMoService = moMoService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<string>> Handle(CreatePaymentUrlCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException($"Không tìm thấy đơn hàng với Id {request.OrderId}");
        }

        if (_currentUserService.UserId.HasValue &&
            _currentUserService.Role != "Admin" &&
            order.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedException("Bạn không có quyền thanh toán cho đơn hàng này.");
        }

        if (order.PaymentStatus == PaymentStatus.Completed)
        {
            return Result<string>.Failure("Đơn hàng này đã được thanh toán thành công.");
        }

        string paymentUrl = string.Empty;

        if (request.Provider == PaymentMethod.VNPay)
        {
            var vnPayReq = new VnPayRequest
            {
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount,
                OrderInfo = $"Thanh toan don hang {order.OrderNumber} qua VNPay",
                IpAddress = string.IsNullOrWhiteSpace(request.IpAddress) ? "127.0.0.1" : request.IpAddress
            };

            paymentUrl = _vnPayService.CreatePaymentUrl(vnPayReq);
        }
        else if (request.Provider == PaymentMethod.MoMo)
        {
            var momoReq = new MoMoPaymentRequest
            {
                OrderId = order.OrderNumber,
                OrderInfo = $"Thanh toán đơn hàng #{order.OrderNumber}",
                Amount = order.TotalAmount
            };

            var momoRes = await _moMoService.CreatePaymentAsync(momoReq, cancellationToken);
            if (!momoRes.IsSuccess || string.IsNullOrEmpty(momoRes.PayUrl))
            {
                return Result<string>.Failure($"Khởi tạo thanh toán MoMo thất bại: {momoRes.Message}");
            }

            paymentUrl = momoRes.PayUrl;
        }
        else
        {
            return Result<string>.Failure("Phương thức thanh toán trực tuyến không được hỗ trợ.");
        }

        return Result<string>.Success(paymentUrl, "Tạo đường dẫn thanh toán thành công.");
    }
}
