using FluentAssertions;
using Moq;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Features.Orders.Commands;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;
using ShopNet.UnitTests.Common;

namespace ShopNet.UnitTests.Features.Orders;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public CancelOrderCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task Handle_PendingOrder_CancelsOrderAndRestoresStock()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var user = new User { Id = 1, Username = "testuser" };
        var product = new Product { Id = 10, Name = "Product 1", Price = 50, StockQuantity = 8 };
        context.Users.Add(user);
        context.Products.Add(product);

        var order = new Order
        {
            UserId = 1,
            OrderNumber = "ORD-TEST-001",
            Status = OrderStatus.Pending,
            TotalAmount = 100
        };
        order.Items.Add(new OrderItem { ProductId = 10, Quantity = 2, UnitPrice = 50 });
        context.Orders.Add(order);

        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);
        _currentUserServiceMock.Setup(u => u.Role).Returns(UserRole.Customer.ToString());

        var handler = new CancelOrderCommandHandler(context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand(order.Id, "Đổi ý không mua nữa");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(OrderStatus.Cancelled.ToString());

        // Stock restored from 8 to 10
        var updatedProduct = context.Products.Find(10);
        updatedProduct!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Handle_DeliveredOrder_ThrowsBadRequestException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User { Username = "delivereduser", Email = "del@test.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Status = OrderStatus.Delivered
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(user.Id);
        _currentUserServiceMock.Setup(u => u.Role).Returns(UserRole.Customer.ToString());

        var handler = new CancelOrderCommandHandler(context, _currentUserServiceMock.Object);
        var command = new CancelOrderCommand(order.Id);

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Không thể hủy đơn hàng*");
    }
}
