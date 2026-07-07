using Application.Common.Security;
using MediatR;

namespace Application.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserQuery : IRequest<AuthUserDto>
{
}
