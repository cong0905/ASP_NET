using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Admin.DTOs;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Admin.Queries;

public record GetAdminDashboardQuery : IRequest<Result<DashboardStatsDto>>;

public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<DashboardStatsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAdminDashboardQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<DashboardStatsDto>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            throw new UnauthorizedException("Chỉ Quản trị viên (Admin) mới có quyền truy cập bảng số liệu này.");
        }

        var totalRevenue = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var totalOrders = await _context.Orders.CountAsync(cancellationToken);
        var pendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
        var totalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer, cancellationToken);
        var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted, cancellationToken);

        // Low stock products (stock <= 15)
        var lowStock = await _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.StockQuantity <= 15)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Description,
                p.SKU,
                p.Price,
                p.StockQuantity,
                p.ImageUrl,
                p.IsActive,
                p.CategoryId,
                p.Category != null ? p.Category.Name : string.Empty,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        // Recent orders
        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new OrderDto(
                o.Id,
                o.OrderNumber,
                o.UserId,
                o.User != null ? o.User.FullName : string.Empty,
                o.OrderDate,
                o.TotalAmount,
                o.Status.ToString(),
                o.ShippingAddress,
                o.PhoneNumber,
                o.Notes,
                o.PaymentMethod.ToString(),
                o.PaymentStatus.ToString(),
                o.Items.Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.UnitPrice,
                    i.Quantity,
                    i.Subtotal
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        // Top selling products by order items
        var topSelling = await _context.OrderItems
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopSellingProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.UnitPrice * x.Quantity)
            ))
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var dashboard = new DashboardStatsDto(
            totalRevenue,
            totalOrders,
            totalCustomers,
            totalProducts,
            pendingOrdersCount,
            topSelling,
            lowStock,
            recentOrders
        );

        return Result<DashboardStatsDto>.Success(dashboard);
    }
}
