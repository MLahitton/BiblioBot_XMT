using System.Security.Claims;
using Microsoft.AspNetCore.Http;

using Application.Common.Interfaces;

namespace Infrastructure.Security;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = CurrentPrincipal;
            if (principal is null)
            {
                return null;
            }

            var userId = principal.FindFirst("sub")?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userId, out var parsed) ? parsed : null;
        }
    }

    public string? Email
    {
        get
        {
            var principal = CurrentPrincipal;
            return principal?.FindFirst(ClaimTypes.Email)?.Value
                ?? principal?.FindFirst("email")?.Value;
        }
    }

    public bool IsAuthenticated
        => CurrentPrincipal?.Identity?.IsAuthenticated ?? false;

    private ClaimsPrincipal? CurrentPrincipal =>
        _httpContextAccessor.HttpContext?.User;
}
