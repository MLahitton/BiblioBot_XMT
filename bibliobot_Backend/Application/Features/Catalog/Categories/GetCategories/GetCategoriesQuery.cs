using Application.Features.Catalog.Categories.Common;
using MediatR;

namespace Application.Features.Catalog.Categories.GetCategories;

public sealed class GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

