using Application.Common.DTOs;
using Application.Features.Books.Common;
using MediatR;

namespace Application.Features.Books.GetBooks;

public sealed class GetBooksQuery : IRequest<PagedResult<BookListItemDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CategoryId { get; set; }
    public Guid? AuthorId { get; set; }
}
