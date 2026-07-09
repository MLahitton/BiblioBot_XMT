using MediatR;

namespace Application.Features.FavoriteBooks.RemoveFavoriteBook;

public sealed class RemoveFavoriteBookCommand : IRequest<bool>
{
    public Guid BookId { get; init; }
}
