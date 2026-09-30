using FluentAssertions;
using Moq;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Features.Products.Commands;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;
using ShopNet.UnitTests.Common;

namespace ShopNet.UnitTests.Features.Products;

public class AddReviewCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public AddReviewCommandHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task Handle_ValidReview_AddsReviewSuccessfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var user = new User { Id = 1, Username = "reviewer", FullName = "Nguyen Van Reviewer" };
        var product = new Product { Id = 5, Name = "Smartphone X", Price = 999, StockQuantity = 10 };
        context.Users.Add(user);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);

        var handler = new AddReviewCommandHandler(context, _currentUserServiceMock.Object);
        var command = new AddReviewCommand(5, 5, "Sản phẩm tuyệt vời!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data!.Rating.Should().Be(5);
        result.Data.Comment.Should().Be("Sản phẩm tuyệt vời!");

        var savedReview = context.ProductReviews.FirstOrDefault(r => r.ProductId == 5 && r.UserId == 1);
        savedReview.Should().NotBeNull();
        savedReview!.Rating.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        _currentUserServiceMock.Setup(u => u.UserId).Returns(1);

        var handler = new AddReviewCommandHandler(context, _currentUserServiceMock.Object);
        var command = new AddReviewCommand(9999, 4, "Comment");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
