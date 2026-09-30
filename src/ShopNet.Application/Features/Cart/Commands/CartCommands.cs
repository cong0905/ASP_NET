using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Cart.DTOs;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Cart.Commands;

public record AddToCartCommand(int ProductId, int Quantity) : IRequest<Result<CartDto>>;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
    }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public AddToCartCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<Result<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var userId = _currentUserService.UserId.Value;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted && p.IsActive, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        if (product.StockQuantity < request.Quantity)
        {
            throw new BadRequestException($"Số lượng hàng trong kho không đủ (Hiện còn: {product.StockQuantity}).");
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            cart = new Domain.Entities.Cart { UserId = userId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            if (product.StockQuantity < (existingItem.Quantity + request.Quantity))
            {
                throw new BadRequestException($"Không thể thêm. Tổng số lượng trong giỏ ({existingItem.Quantity + request.Quantity}) vượt quá tồn kho ({product.StockQuantity}).");
            }
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = product.Price;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitPrice = product.Price
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _sender.Send(new Queries.GetCartQuery(), cancellationToken);
    }
}

public record UpdateCartItemCommand(int ProductId, int Quantity) : IRequest<Result<CartDto>>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public UpdateCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<Result<CartDto>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var userId = _currentUserService.UserId.Value;
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            throw new NotFoundException("Giỏ hàng trống.");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null)
        {
            throw new NotFoundException("Sản phẩm không có trong giỏ hàng.");
        }

        if (request.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var product = await _context.Products.FindAsync(new object[] { request.ProductId }, cancellationToken);
            if (product != null && product.StockQuantity < request.Quantity)
            {
                throw new BadRequestException($"Số lượng trong kho không đủ (Hiện còn: {product.StockQuantity}).");
            }
            item.Quantity = request.Quantity;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await _sender.Send(new Queries.GetCartQuery(), cancellationToken);
    }
}

public record RemoveCartItemCommand(int ProductId) : IRequest<Result<CartDto>>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public RemoveCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var userId = _currentUserService.UserId.Value;
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart != null)
        {
            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item != null)
            {
                cart.Items.Remove(item);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return await _sender.Send(new Queries.GetCartQuery(), cancellationToken);
    }
}
