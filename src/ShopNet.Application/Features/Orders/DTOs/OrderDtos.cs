using ShopNet.Domain.Enums;

namespace ShopNet.Application.Features.Orders.DTOs;

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal
);

public record OrderDto(
    int Id,
    string OrderNumber,
    int UserId,
    string CustomerName,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    string ShippingAddress,
    string PhoneNumber,
    string? Notes,
    string PaymentMethod,
    string PaymentStatus,
    List<OrderItemDto> Items
);
