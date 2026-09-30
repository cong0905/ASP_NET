using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Auth.DTOs;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Auth.Commands;

public record UpdateProfileCommand(
    string FullName,
    string? PhoneNumber,
    string? Address
) : IRequest<Result<UserDto>>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không vượt quá 100 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[0-9\+\-\s]{8,15}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Số điện thoại không hợp lệ.");
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("Người dùng không tồn tại.");
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = request.Address;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role.ToString()
        );

        return Result<UserDto>.Success(dto, "Cập nhật thông tin cá nhân thành công.");
    }
}
