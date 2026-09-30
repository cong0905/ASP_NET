namespace ShopNet.Application.Features.Cart.DTOs;

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal
);

public record CartDto(
    int Id,
    int UserId,
    List<CartItemDto> Items,
    decimal TotalAmount,
    int TotalItems
);
