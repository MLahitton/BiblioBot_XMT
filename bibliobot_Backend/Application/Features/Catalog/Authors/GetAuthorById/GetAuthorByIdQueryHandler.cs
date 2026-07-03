using Application.Common.Interfaces;
using Application.Features.Catalog.Authors.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Authors.GetAuthorById;

public sealed class GetAuthorByIdQueryHandler : IRequestHandler<GetAuthorByIdQuery, AuthorDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAuthorByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorDto?> Handle(
        GetAuthorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _context.Authors.AsNoTracking()
            .Where(author => author.Id == request.Id)
            .Select(author => new AuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                IsActive = author.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}

