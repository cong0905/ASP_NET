using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Products.Queries;

public record GetProductByIdQuery(int Id) : IRequest<Result<ProductDetailDto>>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        var reviews = product.Reviews.Select(r => new ReviewDto(
            r.Id,
            r.ProductId,
            r.UserId,
            r.User?.FullName ?? "Ẩn danh",
            r.Rating,
            r.Comment,
            r.CreatedAt
        )).OrderByDescending(r => r.CreatedAt).ToList();

        var avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

        var dto = new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.SKU,
            product.Price,
            product.StockQuantity,
            product.ImageUrl,
            product.IsActive,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            avgRating,
            reviews.Count,
            reviews,
            product.CreatedAt,
            product.UpdatedAt
        );

        return Result<ProductDetailDto>.Success(dto);
    }
}
