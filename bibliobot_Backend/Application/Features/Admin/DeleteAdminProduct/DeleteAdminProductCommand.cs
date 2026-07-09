using MediatR;

namespace Application.Features.Admin.DeleteAdminProduct;

public sealed class DeleteAdminProductCommand : IRequest<bool>
{
    public Guid Id { get; init; }
}
