using Application.Common.Interfaces;
using Application.Features.Catalog.Authors.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Authors.ActivateAuthor;

public sealed class ActivateAuthorCommandHandler : IRequestHandler<ActivateAuthorCommand, AuthorDto?>
{
    private readonly IApplicationDbContext _context;

    public ActivateAuthorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorDto?> Handle(
        ActivateAuthorCommand request,
        CancellationToken cancellationToken)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(author => author.Id == request.Id, cancellationToken);

        if (author is null)
        {
            return null;
        }

        if (author.IsActive)
        {
            return new AuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                IsActive = author.IsActive,
            };
        }

        author.IsActive = true;
        author.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthorDto
        {
            Id = author.Id,
            FullName = author.FullName,
            IsActive = author.IsActive,
        };
    }
}

