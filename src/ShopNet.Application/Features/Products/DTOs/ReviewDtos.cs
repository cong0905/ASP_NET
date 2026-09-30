namespace ShopNet.Application.Features.Products.DTOs;

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    int Rating,
    string Comment,
    DateTime CreatedAt
);
