using FluentAssertions;
using ShopNet.Application.Features.Products.Commands;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;
using ShopNet.UnitTests.Common;

namespace ShopNet.UnitTests.Features.Products;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesProductSuccessfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Laptops", Slug = "laptops" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand(
            Name: "Gaming Laptop Pro",
            Description: "High-end laptop",
            SKU: "LAP-001",
            Price: 1500m,
            StockQuantity: 10,
            CategoryId: category.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Gaming Laptop Pro");
        result.Data.SKU.Should().Be("LAP-001");
        result.Data.Price.Should().Be(1500m);

        var savedProduct = context.Products.FirstOrDefault(p => p.SKU == "LAP-001");
        savedProduct.Should().NotBeNull();
        savedProduct!.StockQuantity.Should().Be(10);
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsBadRequestException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Laptops", Slug = "laptops" };
        context.Categories.Add(category);
        context.Products.Add(new Product
        {
            Name = "Existing Laptop",
            SKU = "DUPLICATE-SKU",
            Price = 1000,
            StockQuantity = 5,
            CategoryId = category.Id
        });
        await context.SaveChangesAsync();

        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("New Laptop", "Description", "DUPLICATE-SKU", 1200m, 5, category.Id);

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*đã tồn tại*");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("New Laptop", "Description", "SKU-999", 1200m, 5, 9999);

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
