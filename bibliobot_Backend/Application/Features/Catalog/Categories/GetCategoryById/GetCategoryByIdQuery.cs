using Application.Features.Catalog.Categories.Common;
using MediatR;

namespace Application.Features.Catalog.Categories.GetCategoryById;

public sealed class GetCategoryByIdQuery : IRequest<CategoryDto?>
{
    public Guid Id { get; init; }
}

