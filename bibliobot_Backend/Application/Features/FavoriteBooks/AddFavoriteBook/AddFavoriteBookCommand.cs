using Application.Features.FavoriteBooks.Common;
using MediatR;

namespace Application.Features.FavoriteBooks.AddFavoriteBook;

public sealed class AddFavoriteBookCommand : IRequest<FavoriteBookDto>
{
    public Guid BookId { get; init; }
}
