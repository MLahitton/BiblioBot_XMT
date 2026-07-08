using Application.Features.FavoriteBooks.Common;
using MediatR;

namespace Application.Features.FavoriteBooks.GetFavoriteBookStatus;

public sealed class GetFavoriteBookStatusQuery : IRequest<FavoriteBookStatusDto>
{
    public Guid BookId { get; init; }
}
