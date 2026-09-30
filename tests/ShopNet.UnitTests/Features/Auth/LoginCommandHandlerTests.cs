using FluentAssertions;
using Moq;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Features.Auth.Commands;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;
using ShopNet.Domain.Exceptions;
using ShopNet.UnitTests.Common;

namespace ShopNet.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;

    public LoginCommandHandlerTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User
        {
            Username = "johndoe",
            Email = "john@example.com",
            PasswordHash = "hashed_pw",
            FullName = "John Doe",
            Role = UserRole.Customer
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        _passwordHasherMock.Setup(p => p.Verify("secret123", "hashed_pw")).Returns(true);
        _jwtTokenServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock_jwt_token");
        _jwtTokenServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("mock_refresh_token");

        var handler = new LoginCommandHandler(context, _passwordHasherMock.Object, _jwtTokenServiceMock.Object);
        var command = new LoginCommand("john@example.com", "secret123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("mock_jwt_token");
        result.Data.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User
        {
            Username = "johndoe",
            Email = "john@example.com",
            PasswordHash = "hashed_pw",
            FullName = "John Doe",
            Role = UserRole.Customer
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        _passwordHasherMock.Setup(p => p.Verify("wrong_pass", "hashed_pw")).Returns(false);

        var handler = new LoginCommandHandler(context, _passwordHasherMock.Object, _jwtTokenServiceMock.Object);
        var command = new LoginCommand("john@example.com", "wrong_pass");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new LoginCommandHandler(context, _passwordHasherMock.Object, _jwtTokenServiceMock.Object);
        var command = new LoginCommand("nonexistent@example.com", "secret123");

        // Act & Assert
        await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
