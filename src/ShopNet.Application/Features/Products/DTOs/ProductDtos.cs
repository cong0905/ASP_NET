namespace ShopNet.Application.Features.Products.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    string SKU,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    DateTime CreatedAt
);

public record ProductDetailDto(
    int Id,
    string Name,
    string Description,
    string SKU,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    double AverageRating,
    int ReviewCount,
    List<ReviewDto> Reviews,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
