using Application.Features.Books.Common;
using MediatR;

namespace Application.Features.Books.ActivateBook;

public sealed class ActivateBookCommand : IRequest<BookDetailDto?>
{
    public Guid Id { get; init; }
}

