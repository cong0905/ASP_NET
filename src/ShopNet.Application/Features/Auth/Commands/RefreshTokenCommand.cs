using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Auth.DTOs;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string Token,
    string RefreshToken
) : IRequest<Result<AuthResponseDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        user.RefreshToken = _jwtTokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(cancellationToken);

        var newToken = _jwtTokenService.GenerateAccessToken(user);

        var response = new AuthResponseDto(
            newToken,
            user.RefreshToken,
            DateTime.UtcNow.AddHours(2),
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString()
        );

        return Result<AuthResponseDto>.Success(response, "Làm mới token thành công.");
    }
}
