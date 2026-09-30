using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Products.Queries;

public record GetProductsQuery(
    string? Search = null,
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? SortBy = null,
    int PageIndex = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedList<ProductDto>>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PaginatedList<ProductDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.SKU.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        query = request.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "name_asc" => query.OrderBy(p => p.Name),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var projected = query.Select(p => new ProductDto(
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
        ));

        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var paginated = await PaginatedList<ProductDto>.CreateAsync(projected, pageIndex, pageSize, cancellationToken);
        return Result<PaginatedList<ProductDto>>.Success(paginated);
    }
}
