using Application.Common.Interfaces;
using Application.Features.Catalog.Categories.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Categories.ActivateCategory;

public sealed class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommand, CategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public ActivateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto?> Handle(
        ActivateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == request.Id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        if (category.IsActive)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
            };
        }

        category.IsActive = true;
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

