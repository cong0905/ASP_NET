using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Orders.Commands;

public record UpdateOrderStatusCommand(
    int OrderId,
    OrderStatus Status
) : IRequest<Result<OrderDto>>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderDto>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.OrderId);
        }

        order.Status = request.Status;
        if (request.Status == OrderStatus.Delivered)
        {
            order.PaymentStatus = PaymentStatus.Completed;
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

        return Result<OrderDto>.Success(dto, $"Đã cập nhật trạng thái đơn hàng thành {request.Status}.");
    }
}
