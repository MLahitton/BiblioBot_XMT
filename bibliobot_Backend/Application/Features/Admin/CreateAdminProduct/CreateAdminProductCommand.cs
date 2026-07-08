using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.CreateAdminProduct;

public sealed class CreateAdminProductCommand : AdminProductMutation, IRequest<AdminProductDto>
{
}
