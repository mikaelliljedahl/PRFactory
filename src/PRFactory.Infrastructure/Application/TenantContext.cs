using PRFactory.Core.Application.Services;

namespace PRFactory.Infrastructure.Application;

/// <summary>
/// Implementation of tenant context.
/// Currently returns a hardcoded demo tenant ID.
/// In production, this would be replaced with logic to get the tenant from:
/// - HTTP headers
/// - User claims
/// - Session data
/// - Request context
/// </summary>
public class TenantContext : ITenantContext
{
    /// <summary>
    /// Demo tenant ID for development
    /// </summary>
    private static readonly Guid DemoTenantId = new Guid("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc/>
    public Guid GetCurrentTenantId()
    {
        // TODO: In production, get tenant ID from:
        // - IHttpContextAccessor for web requests
        // - User claims/JWT token
        // - Session data
        // - Multi-tenant routing logic

        return DemoTenantId;
    }
}
