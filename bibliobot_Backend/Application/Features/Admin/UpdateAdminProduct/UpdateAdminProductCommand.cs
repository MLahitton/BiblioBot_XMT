using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.UpdateAdminProduct;

public sealed class UpdateAdminProductCommand : AdminProductMutation, IRequest<AdminProductDto?>
{
    public Guid Id { get; init; }
}
