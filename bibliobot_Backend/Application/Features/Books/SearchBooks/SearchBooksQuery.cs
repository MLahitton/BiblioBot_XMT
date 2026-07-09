using Application.Common.DTOs;
using Application.Features.Books.Common;
using MediatR;

namespace Application.Features.Books.SearchBooks;

public sealed class SearchBooksQuery : IRequest<PagedResult<BookListItemDto>>
{
    public string Query { get; init; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
