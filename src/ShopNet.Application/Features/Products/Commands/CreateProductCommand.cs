using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Application.Features.Products.DTOs;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Products.Commands;

public record CreateProductCommand(
    string Name,
    string Description,
    string SKU,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string? ImageUrl = null
) : IRequest<Result<ProductDto>>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống.")
            .MaximumLength(200).WithMessage("Tên sản phẩm không vượt quá 200 ký tự.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("Mã SKU không được để trống.")
            .MaximumLength(50).WithMessage("Mã SKU không vượt quá 50 ký tự.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng kho không thể là số âm.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Danh mục sản phẩm không hợp lệ.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        var skuExists = await _context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken);
        if (skuExists)
        {
            throw new BadRequestException($"Mã SKU '{request.SKU}' đã tồn tại trong hệ thống.");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.SKU,
            product.Price,
            product.StockQuantity,
            product.ImageUrl,
            product.IsActive,
            product.CategoryId,
            category.Name,
            product.CreatedAt
        );

        return Result<ProductDto>.Success(dto, "Thêm sản phẩm thành công.", 201);
    }
}
