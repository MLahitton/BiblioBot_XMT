using Application.Features.Books.Common;
using MediatR;

namespace Application.Features.Books.DisableBook;

public sealed class DisableBookCommand : IRequest<BookDetailDto?>
{
    public Guid Id { get; init; }
}

