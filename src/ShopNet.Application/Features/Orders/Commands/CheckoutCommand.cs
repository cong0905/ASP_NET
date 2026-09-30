using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Orders.Commands;

public record CheckoutCommand(
    string ShippingAddress,
    string PhoneNumber,
    string? Notes = null,
    PaymentMethod PaymentMethod = PaymentMethod.COD
) : IRequest<Result<OrderDto>>;

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Địa chỉ giao hàng không được để trống.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^[0-9\+\-\s]{8,15}$").WithMessage("Số điện thoại không hợp lệ.");
    }
}

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thanh toán.");
        }

        var userId = _currentUserService.UserId.Value;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null || !cart.Items.Any())
        {
            throw new BadRequestException("Giỏ hàng của bạn đang trống, không thể tiến hành đặt hàng.");
        }

        // Validate stock for all items
        foreach (var item in cart.Items)
        {
            if (item.Product == null || item.Product.IsDeleted || !item.Product.IsActive)
            {
                throw new BadRequestException($"Sản phẩm '{item.Product?.Name ?? $"ID {item.ProductId}"}' hiện không còn kinh doanh.");
            }

            if (item.Product.StockQuantity < item.Quantity)
            {
                throw new BadRequestException($"Sản phẩm '{item.Product.Name}' không đủ số lượng trong kho (Hiện còn: {item.Product.StockQuantity}).");
            }
        }

        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = request.ShippingAddress,
            PhoneNumber = request.PhoneNumber,
            Notes = request.Notes,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = request.PaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Completed,
            Status = OrderStatus.Pending
        };

        decimal total = 0;
        foreach (var item in cart.Items)
        {
            var itemTotal = item.Product!.Price * item.Quantity;
            total += itemTotal;

            // Deduct stock
            item.Product.StockQuantity -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity
            });
        }

        order.TotalAmount = total;
        _context.Orders.Add(order);

        // Clear cart
        _context.CartItems.RemoveRange(cart.Items);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new OrderDto(
            order.Id,
            order.OrderNumber,
            order.UserId,
            user.FullName,
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

        return Result<OrderDto>.Success(dto, "Đặt hàng thành công!", 201);
    }
}
