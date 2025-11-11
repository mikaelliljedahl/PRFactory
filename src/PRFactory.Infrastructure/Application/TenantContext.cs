using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of tenant context.
/// Gets the current tenant ID from the authenticated user's context.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly ICurrentUserService _currentUserService;

    public TenantContext(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc/>
    public async Task<Guid> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await _currentUserService.GetCurrentTenantIdAsync(cancellationToken);

        if (tenantId == null)
        {
            throw new UnauthorizedAccessException(
                "User not authenticated or has no tenant. Ensure the user is logged in and has been provisioned.");
        }

        return tenantId.Value;
    }
}
