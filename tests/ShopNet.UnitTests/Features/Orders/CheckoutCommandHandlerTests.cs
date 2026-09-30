using FluentAssertions;
using Moq;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Features.Orders.Commands;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;
using ShopNet.UnitTests.Common;

namespace ShopNet.UnitTests.Features.Orders;

public class CheckoutCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public CheckoutCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderDecrementsStockAndClearsCart()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var user = new User
        {
            Id = 1,
            Username = "customer1",
            Email = "cust@test.com",
            FullName = "Nguyen Van Test"
        };
        context.Users.Add(user);

        var product = new Product
        {
            Id = 10,
            Name = "Mechanical Keyboard",
            SKU = "KB-01",
            Price = 100m,
            StockQuantity = 10,
            IsActive = true
        };
        context.Products.Add(product);

        var cart = new Cart
        {
            Id = 1,
            UserId = 1
        };
        cart.Items.Add(new CartItem
        {
            CartId = 1,
            ProductId = 10,
            Product = product,
            Quantity = 3,
            UnitPrice = 100m
        });
        context.Carts.Add(cart);

        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);

        var handler = new CheckoutCommandHandler(context, _currentUserServiceMock.Object);
        var command = new CheckoutCommand(
            ShippingAddress: "123 Tran Hung Dao, Dist 1, HCMC",
            PhoneNumber: "0901234567",
            PaymentMethod: PaymentMethod.COD
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalAmount.Should().Be(300m);
        result.Data.Items.Should().HaveCount(1);
        result.Data.Items[0].Quantity.Should().Be(3);

        // Verify product stock decremented from 10 to 7
        var updatedProduct = context.Products.Find(10);
        updatedProduct!.StockQuantity.Should().Be(7);

        // Verify cart items are cleared
        var updatedCart = context.Carts.Find(1);
        context.Entry(updatedCart!).Collection(c => c.Items).Load();
        updatedCart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyCart_ThrowsBadRequestException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User { Id = 1, Username = "emptyuser" };
        var cart = new Cart { Id = 1, UserId = 1 };
        context.Users.Add(user);
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);

        var handler = new CheckoutCommandHandler(context, _currentUserServiceMock.Object);
        var command = new CheckoutCommand("Address", "0901234567");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*đang trống*");
    }

    [Fact]
    public async Task Handle_StockExceeded_ThrowsBadRequestException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User { Id = 1, Username = "user1" };
        context.Users.Add(user);

        var product = new Product
        {
            Id = 5,
            Name = "Low Stock Item",
            SKU = "LOW-01",
            Price = 50m,
            StockQuantity = 2,
            IsActive = true
        };
        context.Products.Add(product);

        var cart = new Cart { Id = 1, UserId = 1 };
        cart.Items.Add(new CartItem { CartId = 1, ProductId = 5, Product = product, Quantity = 5, UnitPrice = 50m });
        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);

        var handler = new CheckoutCommandHandler(context, _currentUserServiceMock.Object);
        var command = new CheckoutCommand("Address", "0901234567");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*không đủ số lượng*");
    }
}
