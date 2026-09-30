using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Products.Commands;

public record AddReviewCommand(
    int ProductId,
    int Rating,
    string Comment
) : IRequest<Result<ReviewDto>>;

public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
{
    public AddReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Đánh giá sao phải từ 1 đến 5 sao.");
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Nội dung đánh giá không được để trống.")
            .MaximumLength(1000).WithMessage("Nội dung không vượt quá 1000 ký tự.");
    }
}

public class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Result<ReviewDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddReviewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReviewDto>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để đánh giá sản phẩm.");
        }

        var userId = _currentUserService.UserId.Value;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        var review = new ProductReview
        {
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ReviewDto(
            review.Id,
            review.ProductId,
            review.UserId,
            user?.FullName ?? "Khách hàng",
            review.Rating,
            review.Comment,
            review.CreatedAt
        );

        return Result<ReviewDto>.Success(dto, "Gửi đánh giá thành công!", 201);
    }
}

public record GetProductReviewsQuery(int ProductId) : IRequest<Result<List<ReviewDto>>>;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, Result<List<ReviewDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetProductReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == request.ProductId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.ProductId,
                r.UserId,
                r.User != null ? r.User.FullName : "Ẩn danh",
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<List<ReviewDto>>.Success(reviews);
    }
}
