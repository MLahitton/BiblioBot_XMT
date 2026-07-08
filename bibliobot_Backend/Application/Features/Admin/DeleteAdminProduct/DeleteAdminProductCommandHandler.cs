using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.DeleteAdminProduct;

public sealed class DeleteAdminProductCommandHandler : IRequestHandler<DeleteAdminProductCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteAdminProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteAdminProductCommand request, CancellationToken cancellationToken)
    {
        var book = await _context.Books.FirstOrDefaultAsync(
            current => current.Id == request.Id && !current.IsDeleted,
            cancellationToken);

        if (book is null)
        {
            return false;
        }

        if (book.IsActive)
        {
            throw new InvalidOperationException("Solo se pueden eliminar productos inactivos.");
        }

        var now = DateTimeOffset.UtcNow;
        book.IsDeleted = true;
        book.IsActive = false;
        book.DeletedAt = now;
        book.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
