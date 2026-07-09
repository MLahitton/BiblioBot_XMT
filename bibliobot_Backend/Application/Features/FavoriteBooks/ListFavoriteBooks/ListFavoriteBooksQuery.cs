using Application.Features.FavoriteBooks.Common;
using MediatR;

namespace Application.Features.FavoriteBooks.ListFavoriteBooks;

public sealed class ListFavoriteBooksQuery : IRequest<IReadOnlyCollection<FavoriteBookDto>>
{
}
