using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Auth.DTOs;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Auth.Commands;

public record LoginCommand(
    string EmailOrUsername,
    string Password
) : IRequest<Result<AuthResponseDto>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty().WithMessage("Email hoặc tên đăng nhập không được để trống.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var input = request.EmailOrUsername.Trim().ToLower();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == input || u.Username.ToLower() == input, cancellationToken);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email/Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        user.RefreshToken = _jwtTokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.GenerateAccessToken(user);

        var response = new AuthResponseDto(
            token,
            user.RefreshToken,
            DateTime.UtcNow.AddHours(2),
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString()
        );

        return Result<AuthResponseDto>.Success(response, "Đăng nhập thành công.");
    }
}
