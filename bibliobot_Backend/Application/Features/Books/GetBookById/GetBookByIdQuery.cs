using Application.Features.Books.Common;
using MediatR;

namespace Application.Features.Books.GetBookById;

public sealed class GetBookByIdQuery : IRequest<BookDetailDto?>
{
    public Guid Id { get; init; }
}
