using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Orders.Commands;

public record CancelOrderCommand(int OrderId, string? Reason = null) : IRequest<Result<OrderDto>>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CancelOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        // Only owner or admin can cancel
        if (_currentUserService.Role != UserRole.Admin.ToString() && order.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedException("Bạn không có quyền hủy đơn hàng này.");
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new BadRequestException($"Không thể hủy đơn hàng đang ở trạng thái {order.Status}.");
        }

        // Restore stock
        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                ? $"Lý do hủy: {request.Reason}"
                : $"{order.Notes} | Lý do hủy: {request.Reason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new OrderDto(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.User?.FullName ?? string.Empty,
            order.OrderDate,
            order.TotalAmount,
            order.Status.ToString(),
            order.ShippingAddress,
            order.PhoneNumber,
            order.Notes,
            order.PaymentMethod.ToString(),
            order.PaymentStatus.ToString(),
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.Subtotal
            )).ToList()
        );

        return Result<OrderDto>.Success(dto, "Hủy đơn hàng thành công, số lượng hàng đã được hoàn lại kho.");
    }
}

public record ProcessPaymentCommand(
    int OrderId,
    PaymentMethod PaymentMethod = PaymentMethod.VNPay_Mock,
    string? TransactionRef = null
) : IRequest<Result<OrderDto>>;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProcessPaymentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        if (_currentUserService.Role != UserRole.Admin.ToString() && order.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedException("Bạn không có quyền thanh toán cho đơn hàng này.");
        }

        if (order.PaymentStatus == PaymentStatus.Completed)
        {
            throw new BadRequestException("Đơn hàng này đã được thanh toán trước đó.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BadRequestException("Không thể thanh toán cho đơn hàng đã bị hủy.");
        }

        // Simulate payment completion
        order.PaymentMethod = request.PaymentMethod;
        order.PaymentStatus = PaymentStatus.Completed;
        order.Status = OrderStatus.Paid;

        var refCode = request.TransactionRef ?? $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        order.Notes = string.IsNullOrWhiteSpace(order.Notes)
            ? $"Mã giao dịch: {refCode}"
            : $"{order.Notes} | Mã giao dịch: {refCode}";

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new OrderDto(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.User?.FullName ?? string.Empty,
            order.OrderDate,
            order.TotalAmount,
            order.Status.ToString(),
            order.ShippingAddress,
            order.PhoneNumber,
            order.Notes,
            order.PaymentMethod.ToString(),
            order.PaymentStatus.ToString(),
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.Subtotal
            )).ToList()
        );

        return Result<OrderDto>.Success(dto, $"Thanh toán thành công! Mã giao dịch: {refCode}");
    }
}
