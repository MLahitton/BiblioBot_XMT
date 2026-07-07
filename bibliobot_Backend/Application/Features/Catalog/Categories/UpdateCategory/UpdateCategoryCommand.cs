using Application.Features.Catalog.Categories.Common;
using MediatR;

namespace Application.Features.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryCommand : IRequest<CategoryDto?>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

