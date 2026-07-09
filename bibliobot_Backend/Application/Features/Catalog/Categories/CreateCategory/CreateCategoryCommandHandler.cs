using Application.Common.Interfaces;
using Application.Features.Catalog.Categories.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        if (name.Length > 120)
        {
            throw new ArgumentException("El nombre debe tener máximo 120 caracteres.");
        }

        var normalizedName = name.ToUpperInvariant();
        var exists = await _context.Categories.AnyAsync(
            category => category.Name.ToUpper() == normalizedName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ya existe una categoría con ese nombre.");
        }

        var category = new Category
        {
            Name = name,
            IsActive = true,
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
        };
    }
}

