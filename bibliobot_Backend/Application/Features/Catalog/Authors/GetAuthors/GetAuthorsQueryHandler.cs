using Application.Common.Interfaces;
using Application.Features.Catalog.Authors.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Authors.GetAuthors;

public sealed class GetAuthorsQueryHandler : IRequestHandler<GetAuthorsQuery, IReadOnlyCollection<AuthorDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuthorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AuthorDto>> Handle(
        GetAuthorsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Authors.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(author => author.IsActive);
        }

        return await query
            .OrderBy(author => author.FullName)
            .Select(author => new AuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                IsActive = author.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}

