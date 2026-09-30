using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Exceptions;

namespace ShopNet.Application.Features.Categories.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int ProductCount
);
