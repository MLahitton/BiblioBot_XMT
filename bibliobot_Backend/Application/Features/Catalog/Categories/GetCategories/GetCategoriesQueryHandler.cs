using Application.Common.Interfaces;
using Application.Features.Catalog.Categories.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Categories.GetCategories;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Categories.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        return await query
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                TotalBooks = category.BookCategories
                    .Count(bookCategory =>
                        bookCategory.Book.IsActive &&
                        !bookCategory.Book.IsDeleted),
            })
            .Where(category => request.IncludeInactive || category.TotalBooks > 0)
            .ToListAsync(cancellationToken);
    }
}

