using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Orders.Queries;

public record GetOrdersQuery(
    OrderStatus? Status = null,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<OrderDto>>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<PaginatedList<OrderDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaginatedList<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .AsNoTracking();

        // If not Admin, user only sees their own orders
        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            query = query.Where(o => o.UserId == _currentUserService.UserId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        query = query.OrderByDescending(o => o.OrderDate);

        var projected = query.Select(o => new OrderDto(
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
        ));

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize is < 1 or > 50 ? 10 : request.PageSize;

        var paginated = await PaginatedList<OrderDto>.CreateAsync(projected, pageIndex, pageSize, cancellationToken);
        return Result<PaginatedList<OrderDto>>.Success(paginated);
    }
}

public record GetOrderByIdQuery(int Id) : IRequest<Result<OrderDto>>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", request.Id);
        }

        if (_currentUserService.Role != UserRole.Admin.ToString() && order.UserId != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedException("Bạn không có quyền truy cập đơn hàng này.");
        }

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

        return Result<OrderDto>.Success(dto);
    }
}
