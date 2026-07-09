using Application.Features.Catalog.Categories.Common;
using MediatR;

namespace Application.Features.Catalog.Categories.ActivateCategory;

public sealed class ActivateCategoryCommand : IRequest<CategoryDto?>
{
    public Guid Id { get; init; }
}

