using Application.Features.Catalog.Categories.Common;
using MediatR;

namespace Application.Features.Catalog.Categories.DisableCategory;

public sealed class DisableCategoryCommand : IRequest<CategoryDto?>
{
    public Guid Id { get; init; }
}

