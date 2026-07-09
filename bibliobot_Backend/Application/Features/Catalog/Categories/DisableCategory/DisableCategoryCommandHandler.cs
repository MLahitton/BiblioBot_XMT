using Application.Common.Interfaces;
using Application.Features.Catalog.Categories.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Categories.DisableCategory;

public sealed class DisableCategoryCommandHandler : IRequestHandler<DisableCategoryCommand, CategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public DisableCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto?> Handle(
        DisableCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        if (!category.IsActive)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
            };
        }

        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
        };
    }
}

