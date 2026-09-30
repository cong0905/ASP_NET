using ShopNet.Application.Features.Orders.DTOs;
using ShopNet.Application.Features.Products.DTOs;

namespace ShopNet.Application.Features.Admin.DTOs;

public record TopSellingProductDto(
    int ProductId,
    string ProductName,
    int TotalSold,
    decimal TotalRevenue
);

public record DashboardStatsDto(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    int PendingOrdersCount,
    List<TopSellingProductDto> TopSellingProducts,
    List<ProductDto> LowStockProducts,
    List<OrderDto> RecentOrders
);
